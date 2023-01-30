using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Orca;
using Solana.Unity.Dex;
using Solana.Unity.Dex.Models;
using Solana.Unity.Dex.Quotes;
using Solana.Unity.Rpc.Types;
using Solana.Unity.SDK.Example;
using Solana.Unity.SDK.Utility;
using Solana.Unity.Wallet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    public RawImage logoA;
    
    [SerializeField]
    public RawImage logoB;

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
    private PublicKey _whirlpool;

    // Initialize the dropdowns and select USDC and ORCA as default
    void Start()
    {
        _dex = new OrcaDex(
            WalletH.Instance.Wallet.Account, 
            WalletH.Instance.Wallet.ActiveRpcClient,
            commitment: Commitment.Finalized);
        dropdownTokenA.onValueChanged.AddListener(_ => OptionSelected(dropdownTokenA).Forget());
        dropdownTokenB.onValueChanged.AddListener(_ => OptionSelected(dropdownTokenB).Forget());
        _dropdowns = new[] {dropdownTokenA, dropdownTokenB};
        inputAmountA.onValueChanged.AddListener(delegate { GetSwapQuote(); });
        InitializeDropDowns().Forget();
    }

    public async void StartSwap()
    {
        Debug.Log("Start swap");
        await Swap();
    }

    private async UniTask Swap()
    {
        if(_whirlpool == null || _swapQuote == null) return;
        var tr = await _dex.SwapWithQuote(_whirlpool, _swapQuote);
        var result = await WalletH.Instance.Wallet.SignAndSendTransaction(tr);
        Debug.Log(result.Result);
        await WalletH.Instance.Wallet.ActiveRpcClient.ConfirmTransaction(result.Result, Commitment.Confirmed);
        Debug.Log("Transaction confirmed");
        await UniTask.SwitchToMainThread(); 
        manager.ShowScreen(this,"wallet_screen");
    }

    private void GetSwapQuote()
    {
        _tokenSource?.Cancel();
        _tokenSource?.Dispose();
        _tokenSource = new CancellationTokenSource();
        InputAmountAChanged().Forget();
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
        if(_tokenA is null || _tokenB is null) return;
        try
        {
            var inputAAmount = float.Parse(inputAmountA.text);
            _whirlpool = await _dex.FindWhirlpoolAddress(_tokenA.MintAddress, _tokenB.MintAddress);
            _swapQuote = await _dex
                .GetSwapQuoteFromWhirlpool(_whirlpool, 
                    (BigInteger)(inputAAmount * Math.Pow(10, _tokenA.Decimals)),
                    _tokenA.MintAddress);
            var quote = (double)_swapQuote.EstimatedAmountOut/Math.Pow(10, _tokenB.Decimals);
            await MainThreadDispatcher.Instance().EnqueueAsync(() => { 
                inputAmountB.text = quote.ToString(CultureInfo.InvariantCulture);
            });
        }
        catch (Exception)
        {
            // ignored
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
        #endif
        var texture = await FileLoader.LoadFile<Texture2D>(tokenData.LogoURI);
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
