using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Orca;
using Solana.Unity.Dex;
using Solana.Unity.Dex.Math;
using Solana.Unity.Dex.Models;
using Solana.Unity.Dex.Quotes;
using Solana.Unity.Rpc.Types;
using Solana.Unity.SDK;
using Solana.Unity.SDK.Example;
using Solana.Unity.SDK.Nft;
using Solana.Unity.SDK.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

// ReSharper disable once CheckNamespace

public class SwapScreen : SimpleScreen
{
    
    [SerializeField]
    private TMP_Dropdown dropdownTokenA;
    [SerializeField]
    private TMP_Dropdown dropdownTokenB;
    
    [SerializeField]
    private TMP_InputField inputAmountA;
    [SerializeField]
    private TMP_InputField inputAmountB;
    
    [SerializeField]
    private TMP_Text errorTxt;
    
    [SerializeField]
    private RawImage logoA;
    
    [SerializeField]
    private RawImage logoB;
    
    [SerializeField]
    private Button swapButton;

    private string _symbol = string.Empty;
    private string _prevSymbol = string.Empty;
    private IList<TokenData> _tokens;
    private bool _doReset;
    private object _tokenResolver;
    private TokenData _tokenA;
    private TokenData _tokenB;
    private TMP_Dropdown[] _dropdowns;
    private IDex _dex;
    private CancellationTokenSource _tokenSource;
    private SwapQuote _swapQuote;
    private Pool _whirlpool;

    // Initialize the dropdowns and select USDC and ORCA as default
    void Start()
    {
        _dex = new OrcaDex(
            Web3.Account, 
            Web3.Rpc,
            commitment: Commitment.Finalized);
        dropdownTokenA.onValueChanged.AddListener(_ => OptionSelected(dropdownTokenA).Forget());
        dropdownTokenB.onValueChanged.AddListener(_ => OptionSelected(dropdownTokenB).Forget());
        _dropdowns = new[] {dropdownTokenA, dropdownTokenB};
        inputAmountA.onValueChanged.AddListener(delegate { GetSwapQuote(); });
        InitializeDropDowns().Forget();
        swapButton.onClick.AddListener(StartSwap);
        swapButton.interactable = false;
    }

    private async void StartSwap()
    {
        Debug.Log("Start swap");
        await Swap();
    }

    private async UniTask Swap()
    {
        if(_whirlpool == null || _swapQuote == null) return;
        var tr = await _dex.SwapWithQuote(_whirlpool.Address, _swapQuote);
        
        var result = await Web3.Instance.WalletBase.SignAndSendTransaction(tr);
        Loading.StartLoading();
        var confirmed = await Web3.Instance.WalletBase.ActiveRpcClient.ConfirmTransaction(result.Result, Commitment.Confirmed);
        if (confirmed)
        {
            Debug.Log("Transaction confirmed, see transaction at https://explorer.solana.com/tx/" + result.Result);
            await UniTask.SwitchToMainThread(); 
            manager.ShowScreen(this,"wallet_screen");
        }
        else
        {
            errorTxt.text = "Transaction failed";
            errorTxt.enabled = true;
        }
        Loading.StopLoading();
    }

    private void GetSwapQuote()
    {
        _tokenSource?.Cancel();
        _tokenSource?.Dispose();
        _tokenSource = new CancellationTokenSource();
        UniTask.Create(async () =>
        {
            await InputAmountAChanged();
        }).AttachExternalCancellation(_tokenSource.Token);
    }

    /// <summary>
    /// Calculate the swap quote on input change
    /// </summary>
    private async UniTask InputAmountAChanged()
    {
        if(_tokenA is null || _tokenB is null || inputAmountA.text.IsNullOrEmpty()) return;
        try
        {
            var inputAAmount = float.Parse(inputAmountA.text);
            _whirlpool = await _dex.FindWhirlpoolAddress(_tokenA.MintAddress, _tokenB.MintAddress);
            _swapQuote = await _dex
                .GetSwapQuoteFromWhirlpool(_whirlpool.Address, 
                    DecimalUtil.ToUlong(inputAAmount, _tokenA.Decimals),
                    _tokenA.MintAddress);
            var quote = DecimalUtil.FromBigInteger(_swapQuote.EstimatedAmountOut, _tokenB.Decimals);
            await MainThreadDispatcher.Instance().EnqueueAsync(() => { 
                inputAmountB.text = quote.ToString(CultureInfo.InvariantCulture);
                errorTxt.enabled = false;
            });
            var tokenABalance = await Web3.Rpc.GetTokenBalanceByOwnerAsync(
                Web3.Account.PublicKey, _tokenA.Mint);
            if(tokenABalance.Result == null || tokenABalance.Result.Value.AmountUlong < _swapQuote.EstimatedAmountIn)
            {
                errorTxt.text = $"Not enough {_tokenA.Symbol} to perform this swap";
                errorTxt.enabled = true;
                swapButton.interactable = false;
            }
            else
            {
                swapButton.interactable = true; 
            }
        }
        catch (Exception)
        {
            swapButton.interactable = false;
            if (_whirlpool == null)
            {
                errorTxt.text = "Unable to find a pool for this swap";
                errorTxt.enabled = true;   
            }
        }
    }
    
    private async UniTask InitializeDropDowns()
    {
        _tokens = await _dex.GetTokens();
        _tokens.Add(new TokenData { 
            Symbol = "SOL", 
            Mint = "So11111111111111111111111111111111111111112", 
            Decimals = 9, 
            LogoURI = "https://raw.githubusercontent.com/solana-labs/token-list/main/assets/mainnet/So11111111111111111111111111111111111111112/logo.png"});
        ResetOptions(dropdownTokenA);
        ResetOptions(dropdownTokenB);
    }

    private async Task TokenASelected(TokenData tokenData)
    {
        if(_tokenA?.MintAddress == tokenData.MintAddress) return;
        _tokenA = tokenData;
        await LoadTokenLogo(tokenData, logoA);
        GetSwapQuote();
    }
    
    private async Task TokenBSelected(TokenData tokenData)
    {
        if(_tokenB?.MintAddress == tokenData.MintAddress) return;
        _tokenB = tokenData;
        await LoadTokenLogo(tokenData, logoB);
        GetSwapQuote();
    }
    
    /// <summary>
    /// Load token logo from url
    /// </summary>
    /// <param name="tokenData"></param>
    /// <param name="logo"></param>
    private async UniTask LoadTokenLogo(TokenData tokenData, RawImage logo)
    {
        await UniTask.SwitchToMainThread();
        if(tokenData is null || logo is null) return;
        #if UNITY_WEBGL 
        // Use default token list resolver as coingecko icons have CORS issues 
        var tokenLogoUrl = (await WalletScreen.GetTokenMintResolver())?.Resolve(tokenData.Mint)?.TokenLogoUrl;
        if (tokenLogoUrl != null)
        { 
            tokenData.LogoURI = tokenLogoUrl;
        }
        else
        {
            var tokenInfo = await Nft.TryGetNftData(tokenData.Mint, Web3.Rpc);
            tokenData.LogoURI = tokenInfo?.metaplexData?.data?.offchainData?.default_image;
        }
        #endif
        var tokenDataLogoUri = tokenData.LogoURI;
        var texture = await FileLoader.LoadFile<Texture2D>(tokenDataLogoUri);
        var _texture = FileLoader.Resize(texture, 75, 75);
        FileLoader.SaveToPersistentDataPath(Path.Combine(Application.persistentDataPath, $"{tokenData.Mint}.png"), _texture);
        logo.texture = _texture; 
    }

    #region Dropdown auto completion
    
    void Update()
    {
        foreach (var dropdown in _dropdowns)
        {
            if(dropdown is null) continue;
            if (!dropdown.IsExpanded) continue;
            if (_doReset)
            {
                ResetOptions(dropdown);
            }
            DetectInput();
            if (InputChanged())
            {
                RestrictOptions(dropdown);
            }
        }
    }

    private void RestrictOptions(TMP_Dropdown tmpDropdown)
    {
        tmpDropdown.options = _tokens
            .Where(token => token.Symbol.StartsWith(_symbol, StringComparison.OrdinalIgnoreCase) &&
                            token.Whitelisted && !token.PoolToken)
            .Select(token => new TMP_Dropdown.OptionData(token.Symbol)).ToList();
        tmpDropdown.value = -1;
        if(tmpDropdown.options.Count == 1)
        {
            tmpDropdown.value = 0;
            OptionSelected(tmpDropdown).Forget();
        }else if(tmpDropdown.options.Count == 0)
        {
            _symbol = _symbol.Substring(0, Math.Max(_symbol.Length - 1, 0));
        }
        tmpDropdown.Hide();
        tmpDropdown.Show();
        tmpDropdown.RefreshShownValue();
    }
    
    private void ResetOptions(TMP_Dropdown tmpDropdown)
    {
        _symbol = string.Empty;
        _prevSymbol = string.Empty;
        MainThreadDispatcher.Instance().Enqueue(() =>
        {
            tmpDropdown.options = _tokens
                .Where(token => token.Symbol.StartsWith(_symbol, StringComparison.OrdinalIgnoreCase) &&
                                token.Whitelisted && !token.PoolToken)
                .Select(token => new TMP_Dropdown.OptionData(token.Symbol)).ToList();
            _symbol = string.Empty;
            _prevSymbol = string.Empty;
            if (tmpDropdown == dropdownTokenA)
            {
                var usdcIndex = dropdownTokenA.options.TakeWhile(t => t.text != "USDC").Count();
                dropdownTokenA.value = usdcIndex;
            }
            else
            {
                var orcaIndex = dropdownTokenB.options.TakeWhile(t => t.text != "ORCA").Count();
                dropdownTokenB.value = orcaIndex;
            }
        });
    }
    
    
    private async UniTask OptionSelected(TMP_Dropdown tmpDropdown)
    {
        var selectedValue = tmpDropdown.options[tmpDropdown.value];
        var tokenData = _tokens.Where(t => t.Symbol == selectedValue.text).ToList().First();
        if (tmpDropdown == dropdownTokenA)
        {
            await TokenASelected(tokenData);
        }
        if (tmpDropdown == dropdownTokenB)
        {
            await TokenBSelected(tokenData);
        }
    }

    private bool InputChanged()
    {
        if (_symbol == _prevSymbol) return false;
        _prevSymbol = _symbol;
        return true;
    }

    private void DetectInput()
    {
        foreach(KeyCode vKey in Enum.GetValues(typeof(KeyCode)))
        {
            if (!Input.GetKeyUp(vKey)) continue;
            var l = vKey.ToString().Replace("Alpha", "");;
            if (!IsValidAlphanumeric(l)) continue;
            _symbol += l;
        }
        if (Input.GetKeyUp(KeyCode.Backspace) && _symbol.Length > 0)
        {
            _symbol = _symbol.Substring(0, _symbol.Length - 1);
        }
    }

    private static bool IsValidAlphanumeric(string character)
    {
        if(character.Length != 1)
            return false;
        var rg = new Regex(@"^[a-zA-Z0-9\s,]*$");
        return rg.IsMatch(character);
    }
    
    #endregion 
}
