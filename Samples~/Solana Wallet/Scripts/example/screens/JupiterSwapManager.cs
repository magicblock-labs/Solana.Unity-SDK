using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq; 
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Programs; 
using System.Text;
using TMPro;
using UnityEngine.UI;

public class JupiterSwapManager : MonoBehaviour
{
    [Header("Jupiter Configuration")]
    public string jupiterBaseUrl = "https://api.jup.ag"; 
    public string jupiterApiKey = ""; 

    [Header("Token Configuration")]
    public string wrappedSolMint = "So11111111111111111111111111111111111111112";
    public string usdcMint = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v"; 
    public string playTokenMint = "PLAYs3GSSadH2q2JLS7djp7yzeT75NK78XgrE5YLrfq"; 
    
    [Header("Fee Configuration")]
    public string platformFeeWallet = ""; 
    [Range(0f, 10f)]
    public float platformFeePercent = 0.75f; 

    [Header("Optimized Network Fee Settings")]
    public bool useAutoPriorityFee = true;
    [Range(0, 100000)]
    public int fixedPriorityFeeLamports = 10000; 
    public bool preCheckATA = true;

    [Header("Legacy Transaction Settings")]
    [Tooltip("Limit max accounts to fit in legacy transaction (1232 bytes). Default 20 is safe.")]
    [Range(10, 64)]
    public int maxAccounts = 20; // [FIX] New Parameter
    
    [Tooltip("If true, only allows single-hop swaps. Safest for transaction size but worse price.")]
    public bool onlyDirectRoutes = false; // [FIX] New Parameter

    [Header("UI References")]
    public TMP_InputField inputAmountField;
    public TMP_InputField outputAmountField;
    public TMP_Dropdown inputTokenDropdown;
    public TMP_Dropdown outputTokenDropdown;
    public TMP_Text inputBalanceText;
    public TMP_Text outputBalanceText;

    public GameObject quoteInfoContainer; 

    [Header("Quote Details UI")]
    public TMP_Text exchangeRateText;
    public TMP_Text priceImpactText;
    public TMP_Text minimumReceivedText;
    public TMP_Text feeText; 
    public TMP_Text inputUsdText;
    public TMP_Text outputUsdText;
    public TMP_Text estimatedFeesText; 

    public Button swapButton;
    public Button maxButton;
    public Button reverseButton;
    public Toast toast;
    
    [Header("Swap Settings")]
    [Range(0, 1000)]
    public float slippageBps = 250; 
    public float minSwapAmount = 0.001f; 
    public bool debugMode = true;

    private JObject currentQuote; 
    private Dictionary<string, TokenBalance> tokenBalances = new Dictionary<string, TokenBalance>();
    private bool isUpdatingQuote = false;

    private void Start()
    {
        jupiterBaseUrl = jupiterBaseUrl.Trim().TrimEnd('/');
        if (quoteInfoContainer != null) quoteInfoContainer.SetActive(false);

        InitializeEmptyBalances();
        SetupUI();
        StartCoroutine(WaitForWalletAndRefresh());
    }
    
    private System.Collections.IEnumerator WaitForWalletAndRefresh()
    {
        while (Web3.Account == null)
        {
            yield return new WaitForSeconds(0.5f);
        }
        EnsureWeb3Initialized(); // [FIX] Ensure connection
        RefreshBalances();
        InvokeRepeating(nameof(RefreshBalances), 15f, 15f);
    }

    private bool EnsureWeb3Initialized()
    {
        if (Web3.Instance == null || Web3.Rpc == null)
        {
            if (Web3.Instance != null)
            {
                Web3.Instance.customRpc = "https://api.mainnet-beta.solana.com"; 
                return Web3.Rpc != null;
            }
            return false;
        }
        return true;
    }

    private void InitializeEmptyBalances()
    {
        tokenBalances["SOL"] = new TokenBalance { mint = wrappedSolMint, balance = 0, decimals = 9 };
        tokenBalances["PLAY"] = new TokenBalance { mint = playTokenMint, balance = 0, decimals = 9 };
        tokenBalances["USDC"] = new TokenBalance { mint = usdcMint, balance = 0, decimals = 6 };
    }

    private void SetupUI()
    {
        if (inputAmountField != null) inputAmountField.onValueChanged.AddListener(OnInputAmountChanged);
        if (swapButton != null) swapButton.onClick.AddListener(ExecuteSwap);
        if (maxButton != null) maxButton.onClick.AddListener(SetMaxAmount);
        if (reverseButton != null) reverseButton.onClick.AddListener(ReverseTokens);
        if (inputTokenDropdown != null) inputTokenDropdown.onValueChanged.AddListener((_) => OnTokenSelectionChanged());
        if (outputTokenDropdown != null) outputTokenDropdown.onValueChanged.AddListener((_) => OnTokenSelectionChanged());

        SetupTokenDropdowns();
    }

    private void SetupTokenDropdowns()
    {
        var options = new List<string> { "SOL", "PLAY", "USDC" };
        if (inputTokenDropdown != null) { inputTokenDropdown.ClearOptions(); inputTokenDropdown.AddOptions(options); inputTokenDropdown.value = 0; }
        if (outputTokenDropdown != null) { outputTokenDropdown.ClearOptions(); outputTokenDropdown.AddOptions(options); outputTokenDropdown.value = 1; }
    }

    #region BALANCE MANAGEMENT
    public async void RefreshBalances()
    {
        if (Web3.Account == null) return;
        
        await UpdateTokenBalance(wrappedSolMint, "SOL");
        await UpdateTokenBalance(playTokenMint, "PLAY");
        if (!string.IsNullOrEmpty(usdcMint)) await UpdateTokenBalance(usdcMint, "USDC");
        
        UpdateBalanceDisplays();
        UpdateButtonState(); 
    }

    private async Task UpdateTokenBalance(string mint, string symbol)
    {
        try
        {
            if (mint == wrappedSolMint)
            {
                var result = await Web3.Rpc.GetBalanceAsync(Web3.Account.PublicKey);
                if (result.WasSuccessful)
                {
                    double balance = (double)result.Result.Value / 1_000_000_000;
                    tokenBalances[symbol] = new TokenBalance { mint = mint, balance = balance, decimals = 9 };
                }
            }
            else
            {
                var result = await Web3.Rpc.GetTokenAccountsByOwnerAsync(Web3.Account.PublicKey, mint, null);
                if (result.WasSuccessful && result.Result.Value.Count > 0)
                {
                    var tokenAccount = result.Result.Value[0];
                    double balance = double.Parse(tokenAccount.Account.Data.Parsed.Info.TokenAmount.UiAmountString);
                    int decimals = tokenAccount.Account.Data.Parsed.Info.TokenAmount.Decimals;
                    tokenBalances[symbol] = new TokenBalance { mint = mint, balance = balance, decimals = decimals };
                }
                else
                {
                     tokenBalances[symbol] = new TokenBalance { mint = mint, balance = 0, decimals = 9 };
                }
            }
        }
        catch { }
    }

    private void UpdateBalanceDisplays()
    {
        string inputToken = GetSelectedToken(inputTokenDropdown);
        string outputToken = GetSelectedToken(outputTokenDropdown);

        if (inputBalanceText != null)
            inputBalanceText.text = tokenBalances.ContainsKey(inputToken) ? $"{tokenBalances[inputToken].balance:0.####} " : "--";

        if (outputBalanceText != null)
            outputBalanceText.text = tokenBalances.ContainsKey(outputToken) ? $"{tokenBalances[outputToken].balance:0.####} " : "--";
    }
    #endregion

    #region UI INTERACTIONS
    private void OnInputAmountChanged(string value)
    {
        UpdateButtonState(); 
        if (isUpdatingQuote) return;
        if (IsValidInput(out float amount)) UpdateQuote();
        else ClearQuoteInfo(); 
    }

    private void OnTokenSelectionChanged()
    {
        UpdateBalanceDisplays();
        UpdateButtonState();
        if (IsValidInput(out float amount)) UpdateQuote();
        else ClearQuoteInfo();
    }

    private void SetMaxAmount()
    {
        string inputToken = GetSelectedToken(inputTokenDropdown);
        if (tokenBalances.TryGetValue(inputToken, out TokenBalance tokenData))
        {
            double maxAmount = tokenData.balance;
            double requiredBuffer = CalculateRequiredBuffer(inputToken);
            
            if (maxAmount <= requiredBuffer) 
            {
                maxAmount = 0;
                ShowPopup("Insufficient SOL", $"Need {requiredBuffer:F4} SOL minimum", Color.red);
            }
            else 
            {
                maxAmount = maxAmount - requiredBuffer;
            }

            string format = "0." + new string('#', tokenData.decimals);
            inputAmountField.text = maxAmount.ToString(format);
            OnInputAmountChanged(inputAmountField.text);
        }
    }

    private double CalculateRequiredBuffer(string inputToken)
    {
        double buffer = 0.000005; 
        if (useAutoPriorityFee) buffer += 0.00005; 
        else buffer += fixedPriorityFeeLamports / 1_000_000_000.0;
        
        if (!string.IsNullOrEmpty(platformFeeWallet) && platformFeePercent > 0) buffer += 0.00204; 
        buffer += 0.0001; 
        return buffer;
    }

    private void ReverseTokens()
    {
        if (inputTokenDropdown != null && outputTokenDropdown != null)
        {
            int temp = inputTokenDropdown.value;
            inputTokenDropdown.value = outputTokenDropdown.value;
            outputTokenDropdown.value = temp;
            string tempAmount = inputAmountField.text;
            inputAmountField.text = outputAmountField.text;
            outputAmountField.text = tempAmount;
        }
    }

    private bool IsValidInput(out float amount)
    {
        amount = 0;
        if (string.IsNullOrEmpty(inputAmountField.text)) return false;
        if (!float.TryParse(inputAmountField.text, out amount)) return false;
        if (amount <= 0) return false;

        string inputToken = GetSelectedToken(inputTokenDropdown);
        if (amount < minSwapAmount) return false;
        if (tokenBalances.ContainsKey(inputToken) && amount > tokenBalances[inputToken].balance) return false;

        return true;
    }

    private void UpdateButtonState()
    {
        if (swapButton == null) return;
        TMP_Text btnText = swapButton.GetComponentInChildren<TMP_Text>();
        if (btnText == null) return;

        string inputToken = GetSelectedToken(inputTokenDropdown);
        float amount = 0;
        bool hasInput = float.TryParse(inputAmountField.text, out amount);

        if (!hasInput || amount <= 0) { btnText.text = "Enter Amount"; swapButton.interactable = false; return; }
        if (amount < minSwapAmount) { btnText.text = $"Minimum {minSwapAmount}"; swapButton.interactable = false; return; }
        if (tokenBalances.ContainsKey(inputToken) && amount > tokenBalances[inputToken].balance) { btnText.text = "Insufficient Funds"; swapButton.interactable = false; return; }

        if (!HasSufficientSolForFees(inputToken, amount))
        {
            btnText.text = "Need More SOL";
            swapButton.interactable = false;
            return;
        }

        btnText.text = "Swap";
    }

    private bool HasSufficientSolForFees(string inputToken, float swapAmount)
    {
        if (!tokenBalances.ContainsKey("SOL")) return false;
        double solBalance = tokenBalances["SOL"].balance;
        double requiredSol = CalculateRequiredBuffer(inputToken);
        if (inputToken == "SOL") requiredSol += swapAmount;
        return solBalance >= requiredSol;
    }
    #endregion

    #region QUOTE & SWAP
    private async void UpdateQuote()
    {
        if (isUpdatingQuote) return;
        isUpdatingQuote = true;

        try
        {
            string inputToken = GetSelectedToken(inputTokenDropdown);
            string outputToken = GetSelectedToken(outputTokenDropdown);
            
            if (inputToken == outputToken || !float.TryParse(inputAmountField.text, out float amount) || amount <= 0)
            {
                isUpdatingQuote = false; ClearQuoteInfo(); return;
            }

            string inputMint = GetTokenMint(inputToken);
            string outputMint = GetTokenMint(outputToken);
            
            int decimals = tokenBalances.ContainsKey(inputToken) ? tokenBalances[inputToken].decimals : 9;
            ulong amountRaw = (ulong)(amount * Math.Pow(10, decimals));

            if(debugMode) Debug.Log($"[Jupiter] Fetching Quote: {amount} {inputToken} -> {outputToken}");
            currentQuote = await GetQuoteWithRetry(inputMint, outputMint, amountRaw);

            if (currentQuote != null) await DisplayQuoteInfo();
            else 
            {
                ShowPopup("No Route", "Try different tokens", Color.red);
                ClearQuoteInfo();
            }
        }
        catch (Exception e) 
        { 
            Debug.LogError($"[Jupiter] Quote Error: {e.Message}");
            ClearQuoteInfo(); 
        }
        finally { isUpdatingQuote = false; }
    }

    private async Task<JObject> GetQuoteWithRetry(string inputMint, string outputMint, ulong amountRaw)
    {
        bool isValidFeeWallet = !string.IsNullOrEmpty(platformFeeWallet) && platformFeeWallet.Length > 30;
        int feeBps = isValidFeeWallet ? Mathf.RoundToInt(platformFeePercent * 100) : 0;
        string feeParam = feeBps > 0 ? $"&platformFeeBps={feeBps}" : "";

        // Use legacy transactions for mobile wallet compatibility; raw byte signing in editor
        // path avoids the Deserialize/Serialize size inflation
        string routeParams = $"&asLegacyTransaction=true&maxAccounts={maxAccounts}";
        if (onlyDirectRoutes) routeParams += "&onlyDirectRoutes=true";

        string url = $"{jupiterBaseUrl}/swap/v1/quote?inputMint={inputMint}&outputMint={outputMint}&amount={amountRaw}&slippageBps={slippageBps}{feeParam}{routeParams}";
        
        if (debugMode) Debug.Log($"[Jupiter] Quote URL: {url}");
        
        return await GetQuote(url);
    }

    private async Task DisplayQuoteInfo()
    {
        if (currentQuote == null) return;
        try
        {
            if (quoteInfoContainer != null) quoteInfoContainer.SetActive(true);
            if (swapButton != null) swapButton.interactable = true;

            string inputToken = GetSelectedToken(inputTokenDropdown);
            string outputToken = GetSelectedToken(outputTokenDropdown);

            int inDecimals = tokenBalances.ContainsKey(inputToken) ? tokenBalances[inputToken].decimals : 9;
            int outDecimals = tokenBalances.ContainsKey(outputToken) ? tokenBalances[outputToken].decimals : 9;

            double inAmount = double.Parse(currentQuote["inAmount"].ToString()) / Math.Pow(10, inDecimals);
            double outAmount = double.Parse(currentQuote["outAmount"].ToString()) / Math.Pow(10, outDecimals);
            double minReceived = double.Parse(currentQuote["otherAmountThreshold"].ToString()) / Math.Pow(10, outDecimals);

            if (outputAmountField != null) outputAmountField.text = outAmount.ToString($"F{Math.Min(6, outDecimals)}");

            if (exchangeRateText != null && inAmount > 0)
            {
                double rate = outAmount / inAmount;
                exchangeRateText.text = $"1 {inputToken} ≈ {rate:F4} {outputToken}";
            }

            double priceImpact = 0;
            if (priceImpactText != null && currentQuote["priceImpactPct"] != null)
            {
                priceImpact = double.Parse(currentQuote["priceImpactPct"].ToString());
                double displayImpact = priceImpact * 100;
                priceImpactText.text = $"{displayImpact:F2}%";
                
                if (displayImpact < 0.1) priceImpactText.color = Color.green;
                else if (displayImpact < 1.0) priceImpactText.color = new Color(1f, 0.64f, 0f);
                else priceImpactText.color = Color.red;
            }

            if (minimumReceivedText != null) minimumReceivedText.text = $"{minReceived:F4} {outputToken}";
            if (feeText != null) feeText.text = $"{platformFeePercent}%";

            if (estimatedFeesText != null)
            {
                var feeEstimate = await EstimateSwapFees();
                estimatedFeesText.text = $"Network Fees: ~{feeEstimate.totalSolFee:F6} SOL";
            }

            if (currentQuote["swapUsdValue"] != null)
            {
                string usdValStr = currentQuote["swapUsdValue"].ToString();
                if (double.TryParse(usdValStr, out double usdVal))
                {
                    if (inputUsdText != null) inputUsdText.text = $"≈ ${usdVal:F2}";
                    if (outputUsdText != null) 
                    {
                        double outputUsdVal = usdVal * (1.0 - priceImpact);
                        outputUsdText.text = $"≈ ${outputUsdVal:F2}";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Jupiter] Display Error: {ex.Message}");
        }
    }

    private void ClearQuoteInfo()
    {
        if (quoteInfoContainer != null) quoteInfoContainer.SetActive(false);
        if (outputAmountField != null) outputAmountField.text = "";
        
        if (exchangeRateText != null) exchangeRateText.text = "--";
        if (priceImpactText != null) priceImpactText.text = "--";
        if (minimumReceivedText != null) minimumReceivedText.text = "--";
        if (feeText != null) feeText.text = "--";
        if (inputUsdText != null) inputUsdText.text = "";
        if (outputUsdText != null) outputUsdText.text = "";
        if (estimatedFeesText != null) estimatedFeesText.text = "";

        if (swapButton != null) swapButton.interactable = false;
        UpdateButtonState(); 
    }

    private async void ExecuteSwap()
    {
        if (currentQuote == null) return;
        
        if (!EnsureWeb3Initialized()) 
        {
            ShowPopup("System Error", "Connection lost.", Color.red);
            return;
        }

        PublicKey buyerKey = Web3.Account?.PublicKey;
        if (buyerKey == null)
        {
            ShowPopup("Wallet", "Connect wallet first.", Color.red);
            return;
        }

        string inputToken = GetSelectedToken(inputTokenDropdown);
        if (!float.TryParse(inputAmountField.text, out float swapAmount))
        {
            ShowPopup("Error", "Invalid amount", Color.red);
            return;
        }

        if (!HasSufficientSolForFees(inputToken, swapAmount))
        {
            double requiredSol = CalculateRequiredBuffer(inputToken);
            if (inputToken == "SOL") requiredSol += swapAmount;
            ShowPopup("Insufficient SOL", $"Need {requiredSol:F4} SOL total", Color.red);
            return;
        }

        swapButton.interactable = false;
        ShowPopup("Creating Transaction", "Please wait...", Color.yellow);

        try
        {
            string txBase64 = await GetSwapTransaction(currentQuote);
            if (string.IsNullOrEmpty(txBase64)) 
            { 
                ShowPopup("Error", "Failed to create transaction", Color.red); 
                swapButton.interactable = true;
                return; 
            }

            if (debugMode)
            {
                byte[] rawCheck = Convert.FromBase64String(txBase64);
                Debug.Log($"[Jupiter] Transaction size: {rawCheck.Length} bytes");
            }

            ShowPopup("Confirm Swap", "Processing...", Color.yellow);
            await SignAndSendTransaction(txBase64);
        }
        catch (Exception ex) 
        { 
            ShowPopup("Failed", ex.Message, Color.red); 
            swapButton.interactable = true;
        }
    }

    private async void HandleSwapSuccess(string signature)
    {
        ShowPopup("Success!", "Swap Completed!", Color.green);
        if(debugMode) Debug.Log($"[Jupiter] TX Sent: {signature}");
        if (inputAmountField != null) inputAmountField.text = "";
        ClearQuoteInfo();
        await Task.Delay(2000);
        RefreshBalances();
        swapButton.interactable = true;
    }

    private async Task<JObject> GetQuote(string url)
    {
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            if(!string.IsNullOrEmpty(jupiterApiKey)) req.SetRequestHeader("x-api-key", jupiterApiKey);
            await req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success) return JObject.Parse(req.downloadHandler.text); 
            else return null;
        }
    }

    private async Task<string> GetSwapTransaction(JObject quote)
    {
        string feeAccount = null;
        if (!string.IsNullOrEmpty(platformFeeWallet) && platformFeePercent > 0)
        {
            try 
            {
                string outputToken = GetSelectedToken(outputTokenDropdown);
                string outputMint = GetTokenMint(outputToken);
                PublicKey feeWalletKey = new PublicKey(platformFeeWallet);
                PublicKey mintKey = new PublicKey(outputMint);
                PublicKey ata = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(feeWalletKey, mintKey);
                feeAccount = ata.ToString();
                if (preCheckATA && !(await CheckATAExists(ata)))
                {
                    if(debugMode) Debug.Log($"[Jupiter] ATA will be created");
                }
            }
            catch (Exception ex) { if(debugMode) Debug.LogWarning($"[Jupiter] ATA check failed: {ex.Message}"); }
        }

        object priorityFee = useAutoPriorityFee ? "auto" : fixedPriorityFeeLamports;

        var reqData = new 
        {
            quoteResponse = quote, 
            userPublicKey = Web3.Account.PublicKey.ToString(),
            wrapAndUnwrapSol = true,
            asLegacyTransaction = true,
            dynamicComputeUnitLimit = true,
            computeUnitPriceMicroLamports = priorityFee,
            feeAccount = feeAccount 
        };

        string json = JsonConvert.SerializeObject(reqData, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        
        using (UnityWebRequest req = new UnityWebRequest($"{jupiterBaseUrl}/swap/v1/swap", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            if(!string.IsNullOrEmpty(jupiterApiKey)) req.SetRequestHeader("x-api-key", jupiterApiKey);

            await req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var responseObj = JObject.Parse(req.downloadHandler.text);
                return responseObj["swapTransaction"].ToString();
            }
            else
            {
                Debug.LogError($"[Jupiter] Swap Error: {req.error}");
                return null;
            }
        }
    }

    private async Task SignAndSendTransaction(string txBase64)
    {
        try
        {
            byte[] txBytes = Convert.FromBase64String(txBase64);
            RequestResult<string> result = null;

#if UNITY_EDITOR
            // PATH A: EDITOR - Sign raw bytes directly (no Deserialize/Serialize inflation)
            // Transaction format: [numSignatures (1 byte)] [signatures (N * 64 bytes)] [message (remaining bytes)]
            Account editorAccount = Web3.Account;
            if (editorAccount == null)
            {
                ShowPopup("Error", "No editor wallet found.", Color.red);
                swapButton.interactable = true;
                return;
            }

            ShowPopup("Wallet", "Signing with Editor Wallet...", Color.yellow);

            int numSignatures = txBytes[0];
            int messageOffset = 1 + (numSignatures * 64);
            byte[] message = new byte[txBytes.Length - messageOffset];
            Array.Copy(txBytes, messageOffset, message, 0, message.Length);

            // Sign the message and write signature into the first slot (fee payer)
            byte[] signature = editorAccount.Sign(message);
            Array.Copy(signature, 0, txBytes, 1, 64);

            if (debugMode) Debug.Log($"[Jupiter] Signed tx: {txBytes.Length} bytes (no size change)");

            string signedBase64 = Convert.ToBase64String(txBytes);
            result = await Web3.Rpc.SendTransactionAsync(signedBase64);
#else
            // PATH B: MOBILE (Android/iOS) - Use wallet adapter for native signing
            if (Web3.Wallet == null)
            {
                ShowPopup("Error", "No wallet connected.", Color.red);
                swapButton.interactable = true;
                return;
            }

            ShowPopup("Wallet", "Please sign on device...", Color.yellow);
            var transaction = Transaction.Deserialize(txBytes);
            result = await Web3.Wallet.SignAndSendTransaction(transaction);
#endif

            if (result != null && result.WasSuccessful)
            {
                HandleSwapSuccess(result.Result);
            }
            else
            {
                string errorMsg = result != null ? result.Reason : "Unknown Error";
                Debug.LogError($"Transaction failed: {errorMsg}");
                ShowPopup("Failed", "Transaction failed.", Color.red);
                swapButton.interactable = true;
            }
        }
        catch (Exception ex)
        {
            ShowPopup("Error", "Signing Failed", Color.red);
            swapButton.interactable = true;
            Debug.LogError($"[Jupiter] Sign Error: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private async Task<bool> CheckATAExists(PublicKey ataAddress)
    {
        try
        {
            var result = await Web3.Rpc.GetAccountInfoAsync(ataAddress.ToString());
            return result.WasSuccessful && result.Result.Value != null;
        }
        catch { return false; }
    }

    public async Task<FeeEstimate> EstimateSwapFees()
    {
        var estimate = new FeeEstimate();
        estimate.baseFee = 0.000005;
        estimate.priorityFee = useAutoPriorityFee ? 0.00005 : fixedPriorityFeeLamports / 1_000_000_000.0;
        
        if (float.TryParse(inputAmountField.text, out float amount))
        {
            estimate.platformFeeAmount = amount * (platformFeePercent / 100.0);
        }
        
        if (!string.IsNullOrEmpty(platformFeeWallet) && platformFeePercent > 0)
        {
            try
            {
                string outputToken = GetSelectedToken(outputTokenDropdown);
                string outputMint = GetTokenMint(outputToken);
                PublicKey feeWalletKey = new PublicKey(platformFeeWallet);
                PublicKey mintKey = new PublicKey(outputMint);
                PublicKey ata = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(feeWalletKey, mintKey);
                
                if (!(await CheckATAExists(ata)))
                {
                    estimate.ataCreationFee = 0.00203928;
                }
            }
            catch { }
        }
        
        estimate.totalSolFee = estimate.baseFee + estimate.priorityFee + estimate.ataCreationFee;
        return estimate;
    }

    private string GetSelectedToken(TMP_Dropdown d) => d == null ? "SOL" : d.options[d.value].text;
    private string GetTokenMint(string t) => t == "SOL" ? wrappedSolMint : (t == "USDC" ? usdcMint : playTokenMint);
    private void ShowPopup(string t, string m, Color c) { if (toast != null) toast.ShowToast($"{t}: {m}", 3); else Debug.Log($"{t}: {m}"); }
    
    [Serializable] public class TokenBalance { public string mint; public double balance; public int decimals; }
    [Serializable] public class FeeEstimate { public double baseFee; public double priorityFee; public double platformFeeAmount; public double ataCreationFee; public double totalSolFee; }
    #endregion
}
