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
    public static readonly string NativeMint = "So11111111111111111111111111111111111111112";
    
    [SerializeField]
    protected TMP_Dropdown dropdownTokenA;
    [SerializeField]
    protected TMP_Dropdown dropdownTokenB;
    
    [SerializeField]
    protected TMP_InputField inputAmountA;
    [SerializeField]
    protected TMP_InputField inputAmountB;
    
    [SerializeField]
    protected TMP_Text errorTxt;
    
    [SerializeField]
    protected RawImage logoA;
    
    [SerializeField]
    protected RawImage logoB;
    
    [SerializeField]
    protected Button swapButton;

    protected string symbol = string.Empty;
    protected string prevSymbol = string.Empty;
    protected IList<TokenData> tokens;
    protected bool doReset;
    protected TokenData tokenA;
    protected TokenData tokenB;
    protected TMP_Dropdown[] dropdowns;
    protected IDex dex;
    protected CancellationTokenSource tokenSource;
    
    private SwapQuote _swapQuote;
    private Pool _whirlpool;

    // Initialize the dropdowns and select USDC and ORCA as default
    protected virtual void Start()
    {
        dex = new OrcaDex(
            Web3.Account, 
            Web3.Rpc,
            commitment: Commitment.Finalized);
        dropdownTokenA.onValueChanged.AddListener(_ => OptionSelected(dropdownTokenA).Forget());
        dropdownTokenB.onValueChanged.AddListener(_ => OptionSelected(dropdownTokenB).Forget());
        dropdowns = new[] {dropdownTokenA, dropdownTokenB};
        inputAmountA.onValueChanged.AddListener(delegate { GetSwapQuote(); });
        InitializeDropDowns().Forget();
        swapButton.onClick.AddListener(StartSwap);
        swapButton.interactable = false;
    }

    protected async void StartSwap()
    {
        Debug.Log("Start swap");
        await Swap();
    }

    protected virtual async UniTask Swap()
    {
        if(_whirlpool == null || _swapQuote == null) return;
        var tr = await dex.SwapWithQuote(_whirlpool.Address, _swapQuote);
        
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

    protected void GetSwapQuote()
    {
        tokenSource?.Cancel();
        tokenSource?.Dispose();
        tokenSource = new CancellationTokenSource();
        UniTask.Create(async () =>
        {
            await InputAmountAChanged();
        }).AttachExternalCancellation(tokenSource.Token);
    }

    /// <summary>
    /// Calculate the swap quote on input change
    /// </summary>
    protected virtual async UniTask InputAmountAChanged()
    {
        if(tokenA is null || tokenB is null || inputAmountA.text.IsNullOrEmpty()) return;
        try
        {
            var inputAAmount = float.Parse(inputAmountA.text);
            _whirlpool = await dex.FindWhirlpoolAddress(tokenA.MintAddress, tokenB.MintAddress);
            _swapQuote = await dex
                .GetSwapQuoteFromWhirlpool(_whirlpool.Address, 
                    DecimalUtil.ToUlong(inputAAmount, tokenA.Decimals),
                    tokenA.MintAddress);
            var quote = DecimalUtil.FromBigInteger(_swapQuote.EstimatedAmountOut, tokenB.Decimals);
            await MainThreadDispatcher.Instance().EnqueueAsync(() => { 
                inputAmountB.text = quote.ToString(CultureInfo.InvariantCulture);
                errorTxt.enabled = false;
            });
            var tokenABalance = await Web3.Rpc.GetTokenBalanceByOwnerAsync(
                Web3.Account.PublicKey, tokenA.Mint);
            if((tokenA.Mint != NativeMint && (tokenABalance.Result == null || tokenABalance.Result.Value.AmountUlong < _swapQuote.EstimatedAmountIn)) || 
               (tokenA.Mint == NativeMint && (await Web3.Rpc.GetBalanceAsync(Web3.Account.PublicKey)).Result.Value < _swapQuote.EstimatedAmountIn))
            {
                errorTxt.text = $"Not enough {tokenA.Symbol} to perform this swap";
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
    
    protected virtual async UniTask InitializeDropDowns()
    {
        tokens = await dex.GetTokens();
        tokens.Add(new TokenData { 
            Symbol = "SOL", 
            Mint = NativeMint, 
            Decimals = 9, 
            LogoURI = "https://raw.githubusercontent.com/solana-labs/token-list/main/assets/mainnet/So11111111111111111111111111111111111111112/logo.png"});
        ResetOptions(dropdownTokenA);
        ResetOptions(dropdownTokenB);
    }

    protected async Task TokenASelected(TokenData tokenData)
    {
        if(tokenA?.MintAddress == tokenData.MintAddress) return;
        tokenA = tokenData;
        await LoadTokenLogo(tokenData, logoA);
        GetSwapQuote();
    }
    
    protected async Task TokenBSelected(TokenData tokenData)
    {
        if(tokenB?.MintAddress == tokenData.MintAddress) return;
        tokenB = tokenData;
        await LoadTokenLogo(tokenData, logoB);
        GetSwapQuote();
    }
    
    /// <summary>
    /// Load token logo from url
    /// </summary>
    /// <param name="tokenData"></param>
    /// <param name="logo"></param>
    protected async UniTask LoadTokenLogo(TokenData tokenData, RawImage logo)
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
        foreach (var dropdown in dropdowns)
        {
            if(dropdown is null) continue;
            if (!dropdown.IsExpanded) continue;
            if (doReset)
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

    protected virtual void RestrictOptions(TMP_Dropdown tmpDropdown)
    {
        tmpDropdown.options = tokens
            .Where(token => token.Symbol.StartsWith(symbol, StringComparison.OrdinalIgnoreCase) &&
                            token.Whitelisted && !token.PoolToken)
            .Select(token => new TMP_Dropdown.OptionData(token.Symbol)).ToList();
        tmpDropdown.value = -1;
        if(tmpDropdown.options.Count == 1)
        {
            tmpDropdown.value = 0;
            OptionSelected(tmpDropdown).Forget();
        }else if(tmpDropdown.options.Count == 0)
        {
            symbol = symbol.Substring(0, Math.Max(symbol.Length - 1, 0));
        }
        tmpDropdown.Hide();
        tmpDropdown.Show();
        tmpDropdown.RefreshShownValue();
    }
    
    protected virtual void ResetOptions(TMP_Dropdown tmpDropdown)
    {
        symbol = string.Empty;
        prevSymbol = string.Empty;
        MainThreadDispatcher.Instance().Enqueue(() =>
        {
            tmpDropdown.options = tokens
                .Where(token => token.Symbol.StartsWith(symbol, StringComparison.OrdinalIgnoreCase) &&
                                token.Whitelisted && !token.PoolToken)
                .Select(token => new TMP_Dropdown.OptionData(token.Symbol)).ToList();
            symbol = string.Empty;
            prevSymbol = string.Empty;
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
    
    
    protected async UniTask OptionSelected(TMP_Dropdown tmpDropdown)
    {
        var selectedValue = tmpDropdown.options[tmpDropdown.value];
        var tokenData = tokens.Where(t => t.Symbol == selectedValue.text).ToList().First();
        if (tmpDropdown == dropdownTokenA)
        {
            await TokenASelected(tokenData);
        }
        if (tmpDropdown == dropdownTokenB)
        {
            await TokenBSelected(tokenData);
        }
    }

    protected bool InputChanged()
    {
        if (symbol == prevSymbol) return false;
        prevSymbol = symbol;
        return true;
    }

    protected void DetectInput()
    {
        foreach(KeyCode vKey in Enum.GetValues(typeof(KeyCode)))
        {
            if (!Input.GetKeyUp(vKey)) continue;
            var l = vKey.ToString().Replace("Alpha", "");;
            if (!IsValidAlphanumeric(l)) continue;
            symbol += l;
        }
        if (Input.GetKeyUp(KeyCode.Backspace) && symbol.Length > 0)
        {
            symbol = symbol.Substring(0, symbol.Length - 1);
        }
    }

    protected static bool IsValidAlphanumeric(string character)
    {
        if(character.Length != 1)
            return false;
        var rg = new Regex(@"^[a-zA-Z0-9\s,]*$");
        return rg.IsMatch(character);
    }
    
    #endregion 
}
