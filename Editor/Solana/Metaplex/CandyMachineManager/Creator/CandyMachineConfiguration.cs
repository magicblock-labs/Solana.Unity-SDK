using CandyMachineV2.Types;
using UnityEngine;

namespace Solana.Unity.SDK.Editor
{
    [CreateAssetMenu(menuName = "Config")]
    public class CandyMachineConfiguration : ScriptableObject
    {

        [SetupQuestion("How many NFTs will be in your CandyMachine?")]
        public int amount;

        [SetupQuestion("What is the symbol of your collection? Leave empty for no symbol.")]
        public string symbol;       
        
        [SetupQuestion("What is the seller fee basis points?")]
        public int sellerFeeBasisPoints;

        [SetupQuestion("Do you want to use a sequential mint index generation? We recommend you choose no.")]
        public bool sequential;

        [SetupQuestion("What are the end settings?")]
        public EndSettingType endSettingsType;
        public int endSettingsValue;
    }
}
