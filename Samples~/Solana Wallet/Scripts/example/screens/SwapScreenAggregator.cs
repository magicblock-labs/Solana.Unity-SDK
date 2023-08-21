using System;
using System.Globalization;
using System.Linq;
using Cysharp.Threading.Tasks;
using Solana.Unity.Dex;
using Solana.Unity.Dex.Jupiter;
using Solana.Unity.Dex.Math;
using Solana.Unity.Dex.Models;
using Solana.Unity.Dex.Quotes;
using Solana.Unity.Rpc.Types;
using Solana.Unity.SDK;
using TMPro;
using UnityEngine;

// ReSharper disable once CheckNamespace

public class SwapScreenAggregator : SwapScreen
{
    [SerializeField]
    private TMP_Text routeTxt;
    
    private IDexAggregator _dexAg;
    private SwapQuoteAg _swapQuoteAg;

    // Initialize the dropdowns and select USDC and ORCA as default
    protected override void Start()
    {
        _dexAg = new JupiterDexAg(Web3.Account);
        dropdownTokenA.onValueChanged.AddListener(_ => OptionSelected(dropdownTokenA).Forget());
        dropdownTokenB.onValueChanged.AddListener(_ => OptionSelected(dropdownTokenB).Forget());
        dropdowns = new[] {dropdownTokenA, dropdownTokenB};
        inputAmountA.onValueChanged.AddListener(delegate { GetSwapQuote(); });
        InitializeDropDowns().Forget();
        swapButton.onClick.AddListener(StartSwap);
        swapButton.interactable = false;
    }

    /// <summary>
    /// Calculate the swap quote on input change
    /// </summary>
    protected override async UniTask InputAmountAChanged()
    {
        if(tokenA is null || tokenB is null || string.IsNullOrEmpty(inputAmountA.text)) return;
        try
        {
            var inputAAmount = float.Parse(inputAmountA.text);
            _swapQuoteAg = await _dexAg.GetSwapQuote(tokenA.MintAddress, tokenB.MintAddress,
                DecimalUtil.ToUlong(inputAAmount, tokenA.Decimals));
            
            var quote = DecimalUtil.FromBigInteger(_swapQuoteAg.OutputAmount, tokenB.Decimals);
            await MainThreadDispatcher.Instance().EnqueueAsync(() => { 
                inputAmountB.text = quote.ToString(CultureInfo.InvariantCulture);
                errorTxt.enabled = false;
            });
            var tokenABalance = await Web3.Rpc.GetTokenBalanceByOwnerAsync(
                Web3.Account.PublicKey, tokenA.Mint);
            if((tokenA.Mint != NativeMint && (tokenABalance.Result == null || tokenABalance.Result.Value.AmountUlong < _swapQuoteAg.InputAmount)) || 
               (tokenA.Mint == NativeMint && (await Web3.Rpc.GetBalanceAsync(Web3.Account.PublicKey)).Result.Value < _swapQuoteAg.InputAmount))
            {
                errorTxt.text = $"Not enough {tokenA.Symbol} to perform this swap";
                errorTxt.enabled = true;
                swapButton.interactable = false;
            }
            else
            {
                swapButton.interactable = true; 
                routeTxt.enabled = true;
                routeTxt.text = string.Join(" -> ", _swapQuoteAg.RoutePlan.Select(p => p.SwapInfo.Label));
            }
        }
        catch (Exception)
        {
            swapButton.interactable = false;
            if (_swapQuoteAg == null)
            {
                errorTxt.text = "Unable to find a quote for this swap";
                errorTxt.enabled = true;   
            }
        }
    }
    
    protected override async UniTask Swap()
    {
        if(_swapQuoteAg == null) return;
        var tr = await _dexAg.Swap(_swapQuoteAg);
        
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

    #region Ui Handlers
    protected override async UniTask InitializeDropDowns()
    {
        tokens = await _dexAg.GetTokens();
        tokens.Add(new TokenData { 
            Symbol = "SOL", 
            Mint = "So11111111111111111111111111111111111111112", 
            Decimals = 9, 
            LogoURI = "https://raw.githubusercontent.com/solana-labs/token-list/main/assets/mainnet/So11111111111111111111111111111111111111112/logo.png"});
        ResetOptions(dropdownTokenA);
        ResetOptions(dropdownTokenB);
    }
    
    protected override void RestrictOptions(TMP_Dropdown tmpDropdown)
    {
        tmpDropdown.options = tokens
            .Where(token => token.Symbol.StartsWith(symbol, StringComparison.OrdinalIgnoreCase))
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
    
    protected override void ResetOptions(TMP_Dropdown tmpDropdown)
    {
        symbol = string.Empty;
        prevSymbol = string.Empty;
        MainThreadDispatcher.Instance().Enqueue(() =>
        {
            tmpDropdown.options = tokens
                .Where(token => token.Symbol.StartsWith(symbol, StringComparison.OrdinalIgnoreCase))
                .Select(token => new TMP_Dropdown.OptionData(token.Symbol)).ToList();
            symbol = string.Empty;
            prevSymbol = string.Empty;
            if (tmpDropdown == dropdownTokenA)
            {
                var solIndex = dropdownTokenA.options.TakeWhile(t => t.text != "SOL").Count();
                dropdownTokenA.value = solIndex;
                OptionSelected(dropdownTokenA).Forget();
            }
            else
            {
                var usdcIndex = dropdownTokenB.options.TakeWhile(t => t.text != "USDC").Count();
                dropdownTokenB.value = usdcIndex;
                dropdownTokenB.RefreshShownValue();
                OptionSelected(dropdownTokenB).Forget();
            }
        });
    }
    
    #endregion
}
