using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK.Example
{
    public class SimpleScreenManager : MonoBehaviour
    {
        public SimpleScreen[] screens;
        private Dictionary<string, SimpleScreen> screensDict = new();

        private void Awake()
        {
            PopulateDictionary();
        }

        private void OnEnable()
        {
            var ratioFitter = gameObject.GetComponent<AspectRatioFitter>();
            if (Screen.width > Screen.height)
            {

                if (ratioFitter == null)
                {
                    ratioFitter = gameObject.AddComponent<AspectRatioFitter>();
                }
                ratioFitter.aspectRatio = 0.9f;
                ratioFitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            }
            else
            {
                var ft = gameObject.GetComponent<AspectRatioFitter>();
                if(ft)
                    Destroy(ft);
            }
        }

        private void PopulateDictionary()
        {
            if (screens != null && screens.Length > 0)
            {
                foreach (SimpleScreen screen in screens)
                {
                    SetupScreen(screen);
                }
                screens[0].gameObject.SetActive(true);
                screens[0].ShowScreen();
            }
        }

        private void SetupScreen(SimpleScreen screen)
        {
            screen.gameObject.SetActive(false);
            screensDict.Add(screen.gameObject.name, screen);
            screen.manager = this;
        }

        public void ShowScreen(SimpleScreen curScreen, SimpleScreen screen)
        {
            curScreen.HideScreen();
            screen.ShowScreen();
        }

        public void ShowScreen(SimpleScreen curScreen, int index)
        {
            curScreen.HideScreen();
            screens[index].ShowScreen();
        }

        public void ShowScreen(SimpleScreen curScreen, string name, object data = null)
        {
            curScreen.HideScreen();
            screensDict[name].ShowScreen(data);
        }
    }

}
