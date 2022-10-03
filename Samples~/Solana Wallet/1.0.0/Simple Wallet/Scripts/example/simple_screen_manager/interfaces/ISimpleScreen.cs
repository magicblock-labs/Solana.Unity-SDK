// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK.Example
{
    public interface ISimpleScreen 
    {
        SimpleScreenManager manager { get; set; }
        void ShowScreen(object data = null);
        void HideScreen();    
    }

}
