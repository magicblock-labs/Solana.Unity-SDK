using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace

public class Toast : MonoBehaviour
{

    [SerializeField]
    private TextMeshProUGUI txt;

    public void ShowToast(string text,
        int duration)
    {
        StartCoroutine(ShowToastCor(text, duration));
    }

    private IEnumerator ShowToastCor(string text,
        int duration)
    {
        var originalColor = txt.color;

        txt.text = text;
        txt.enabled = true;

        //Fade in
        yield return FadeInAndOut(txt, true, 0.5f);

        //Wait for the duration
        float counter = 0;
        while (counter < duration)
        {
            counter += Time.deltaTime;
            yield return null;
        }

        //Fade out
        yield return FadeInAndOut(txt, false, 0.5f);

        txt.enabled = false;
        txt.color = originalColor;
    }

    private static IEnumerator FadeInAndOut(Graphic targetText, bool fadeIn, float duration)
    {
        //Set Values depending on if fadeIn or fadeOut
        float a, b;
        if (fadeIn)
        {
            a = 0f;
            b = 1f;
        }
        else
        {
            a = 1f;
            b = 0f;
        }

        var currentColor = Color.clear;
        var counter = 0f;

        while (counter < duration)
        {
            counter += Time.deltaTime;
            var alpha = Mathf.Lerp(a, b, counter / duration);
            targetText.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
            yield return null;
        }
    }
}
