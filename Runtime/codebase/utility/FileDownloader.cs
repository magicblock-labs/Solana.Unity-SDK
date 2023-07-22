using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Solana.Unity.Metaplex.NFT.Library;
using Solana.Unity.Wallet;
using ThreeDISevenZeroR.UnityGifDecoder;
using ThreeDISevenZeroR.UnityGifDecoder.Model;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK.Utility
{
    
    public class PublicKeyJsonConverter : JsonConverter<PublicKey>
    {
        public override void WriteJson(JsonWriter writer, PublicKey value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        public override PublicKey ReadJson(JsonReader reader, Type objectType, PublicKey existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var pk = serializer.Deserialize<string>(reader);
            if (pk == null) return null;
            return new PublicKey(pk);
        }
    }

    public class CreatorJsonConverter : JsonConverter<Creator>
    {
        public override void WriteJson(JsonWriter writer, Creator value, JsonSerializer serializer)
        {
            writer.WriteValue(value.key.ToString() + "-" + value.share + "-" + value.verified);
        }

        public override Creator ReadJson(JsonReader reader, Type objectType, Creator existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var parse = serializer.Deserialize<string>(reader)?.Split("-");
            if (parse is not { Length: 3 })
                return null;
            return new Creator(new PublicKey(parse[0]), (byte)int.Parse(parse[1]), bool.Parse(parse[2]));
        }
    }
    
    public class CollectionJsonConverter : JsonConverter<Collection>
    {
        public override void WriteJson(JsonWriter writer, Collection value, JsonSerializer serializer)
        {
            writer.WriteValue(value.key.ToString() + "-" + value.verified);
        }

        public override Collection ReadJson(JsonReader reader, Type objectType, Collection existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var parse = serializer.Deserialize<string>(reader)?.Split("-");
            if (parse is not { Length: 2 })
                return null;
            return new Collection(new PublicKey(parse[0]),  bool.Parse(parse[1]));
        }
    }

    public static class FileLoader
    {
        public static async Task<T> LoadFile<T>(string path, string optionalName = "")
        {

            if (typeof(T) == typeof(Texture2D))
            {
                try
                {
                    if (path.ToLower().EndsWith(".gif") || path.ToLower().EndsWith("ext=gif"))
                    {
                        return await LoadGif<T>(path);
                    }
                    else
                    {
                        return await LoadTexture<T>(path);
                    }
                }
                catch (UnityWebRequestException)
                {
                    return default;
                }
            }
            return default;
        }

        private static async Task<T> LoadTexture<T>(string filePath, CancellationToken token = default)
        {
            using var uwr = UnityWebRequestTexture.GetTexture(filePath);
            await uwr.SendWebRequest();

            while (!uwr.isDone && !token.IsCancellationRequested)
            {
                await Task.Yield();
            }

            if (uwr.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
            {
                Debug.Log(uwr.error);
                return default;
            }
            var texture = DownloadHandlerTexture.GetContent(uwr);
            DestroyTexture(((DownloadHandlerTexture)uwr.downloadHandler).texture);
            return (T)Convert.ChangeType(texture, typeof(T));
        }

        private static async UniTask<T> LoadGif<T>(string path, CancellationToken token = default)
        {
            using UnityWebRequest uwr = UnityWebRequest.Get(path);
            await uwr.SendWebRequest();
            
            while (!uwr.isDone && !token.IsCancellationRequested)
            {
                await Task.Yield();
            }

            if (uwr.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(uwr.error);
                return default;
            }

            Texture mainTexture = GetTextureFromGifByteStream(uwr.downloadHandler.data);
            var changeType = (T)Convert.ChangeType(mainTexture, typeof(T));
            return changeType;
        }
        
        private static Texture2D GetTextureFromGifByteStream(byte[] bytes)
        {
            var frameDelays = new List<float>();

            using (var gifStream = new GifStream(bytes))
            {
                while (gifStream.HasMoreData)
                {
                    switch (gifStream.CurrentToken)
                    {
                        case GifStream.Token.Image:
                            GifImage image = gifStream.ReadImage();
                            var frame = new Texture2D(
                                gifStream.Header.width,
                                gifStream.Header.height,
                                TextureFormat.ARGB32, false);

                            frame.SetPixels32(image.colors);
                            frame.Apply();

                            frameDelays.Add(image.SafeDelaySeconds);

                            return frame;

                        case GifStream.Token.Comment:
                            var commentText = gifStream.ReadComment();
                            Debug.Log(commentText);
                            break;

                        default:
                            gifStream.SkipToken(); // Other tokens
                            break;
                    }
                }
            }

            return null;
        }

        public static T LoadFileFromLocalPath<T>(string path)
        {
            if (!File.Exists(path))
                return default;

            var bytes = File.ReadAllBytes(path);

            var texture = new Texture2D(1, 1);
            if (typeof(T) == typeof(Texture2D))
            {
                texture.LoadImage(bytes);
                return (T)Convert.ChangeType(texture, typeof(T));
            }

            var contents = File.ReadAllText(path);
            try
            {
                var serializeOptions = new JsonSerializerSettings()
                {
                    Converters =
                    {
                        new PublicKeyJsonConverter(),
                        new CreatorJsonConverter(),
                        new CollectionJsonConverter()
                    }
                };
                var data = JsonConvert.DeserializeObject<T>(contents, serializeOptions);
                return data;
            }
            catch(Exception e)
            {
                Debug.Log(e.Message);
                return default;
            }
        }

        public static void SaveToPersistentDataPath<T>(string path, T data)
        {
            if (typeof(T) == typeof(Texture2D))
            {
                var dataToByte = ((Texture2D)Convert.ChangeType(data, typeof(Texture2D))).EncodeToPNG();
                File.WriteAllBytes(path, dataToByte);
            }
            else
            {
                var serializeOptions = new JsonSerializerSettings()
                {
                    Converters =
                    {
                        new PublicKeyJsonConverter(),
                        new CreatorJsonConverter(),
                        new CollectionJsonConverter()
                    }
                };
                var dataString = JsonConvert.SerializeObject(data, serializeOptions);
                File.WriteAllText(path, dataString);
            }
        }
        
        /// <summary>
        /// Resize great textures to small, because of performance
        /// </summary>
        /// <param name="texture2D"> Texture to resize</param>
        /// <param name="targetX"> Target width</param>
        /// <param name="targetY"> Target height</param>
        /// <returns></returns>
        public static Texture2D Resize(Texture texture2D, int targetX, int targetY)
        {
            RenderTexture rt = new RenderTexture(targetX, targetY, 24);
            RenderTexture.active = rt;
            Graphics.Blit(texture2D, rt);
            Texture2D result = new Texture2D(targetX, targetY);
            result.ReadPixels(new Rect(0, 0, targetX, targetY), 0, 0);
            result.Apply();
            DestroyTexture(texture2D);
            return result;
        }
        
        private static void DestroyTexture(Texture texture)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(texture);
            }
            else
            {
                Object.DestroyImmediate(texture);
            }
        }

    }

}
