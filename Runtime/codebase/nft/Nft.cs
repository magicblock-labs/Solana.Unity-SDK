using Solana.Unity.SDK.Utility;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Utilities;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK.Nft
{
    [Serializable]
    public class NftImage : iNftFile<Texture2D>
    {
        public string name { get; set; }
        public string extension { get; set; }
        public string externalUrl { get; set; }
        public Texture2D file { get; set; }

        public int heightAndWidth = 75;
    }

    [Serializable]
    public class Nft
    {
        public Metaplex metaplexData;

        public Nft() { }

        public Nft(Metaplex metaplexData)
        {
            this.metaplexData = metaplexData;
        }

        /// <summary>
        /// Returns all data for listed nft
        /// </summary>
        /// <param name="mint"></param>
        /// <param name="connection">Rpc client</param>
        /// <param name="tryUseLocalContent">If use local content for image</param>
        /// <returns></returns>
        public static async Task<Nft> TryGetNftData(string mint, IRpcClient connection, bool tryUseLocalContent = true)
        {
            var metaplexDataPubKey = FindProgramAddress(mint);

            var data = await GetAccountData(metaplexDataPubKey.Key, connection);
            if (tryUseLocalContent)
            { 
                var nft = TryLoadNftFromLocal(mint);
                if (nft != null)
                {
                    return nft;
                }
            }

            if (data?.Data == null || data.Data.Count <= 0) return null;
            var met = new Metaplex().ParseData(data.Data[0]);
            var jsonData = await FileLoader.LoadFile<MetaplexJsonData>(met.data.url);

            var nftImage = new NftImage();
            if (jsonData != null)
            {
                met.data.json = jsonData;
                var texture = await FileLoader.LoadFile<Texture2D>(met.data.json.image);
                var compressedTexture = FileLoader.Resize(texture, 75, 75);
                FileLoader.SaveToPersistentDataPath(Path.Combine(Application.persistentDataPath, $"{mint}.png"), compressedTexture);
                if (compressedTexture)
                {
                    nftImage.externalUrl = jsonData.image;
                    //nftImage.file = Resize(texture, nftImage.heightAndWidth, nftImage.heightAndWidth);
                    nftImage.file = compressedTexture;
                    met.nftImage = nftImage;
                }
            }
            var newNft = new Nft(met);
            FileLoader.SaveToPersistentDataPath(Path.Combine(Application.persistentDataPath, $"{mint}.json"), newNft);
            return newNft;
        }

        /// <summary>
        /// Returns Nft from local machine if it exists
        /// </summary>
        /// <param name="mint"></param>
        /// <returns></returns>
        public static Nft TryLoadNftFromLocal(string mint)
        {
            var local = FileLoader.LoadFileFromLocalPath<Nft>($"{Path.Combine(Application.persistentDataPath, mint)}.json");

            if (local == null) return null;
            var tex = FileLoader.LoadFileFromLocalPath<Texture2D>($"{Path.Combine(Application.persistentDataPath, mint)}.png");
            if (tex)
            {
                local.metaplexData.nftImage = new NftImage();
                local.metaplexData.nftImage.file = tex;
            }
            else
            {
                return null;
            }

            return local;
        }

        /// <summary>
        /// Returns public key of nft
        /// </summary>
        /// <param name="seed"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static PublicKey CreateAddress(List<byte[]> seed)
        {
            var buffer = new List<byte>();

            foreach (var item in seed)
            {
                if (item.Length > 32)
                {
                    throw new Exception("Too long");
                }

                buffer.AddRange(item);
            }

            buffer.AddRange(seed[1]);
            var derive = Encoding.UTF8.GetBytes("ProgramDerivedAddress");
            buffer.AddRange(derive);

            var sha256 = SHA256.Create();
            var hash1 = sha256.ComputeHash(buffer.ToArray());

            if (hash1.IsOnCurve())
            {
                throw new Exception("Not on curve");
            }

            var publicKey = new PublicKey(hash1);
            return publicKey;
        }

        /// <summary>
        /// Returns metaplex data pubkey from mint pubkey and programId
        /// </summary>
        /// <param name="mintPublicKey"></param>
        /// <param name="programId"></param>
        /// <returns></returns>
        public static PublicKey FindProgramAddress(string mintPublicKey, string programId = "metaqbxxUerdq28cj1RbAWkYQm3ybzjb6a8bt518x1s")
        {
            List<byte[]> seeds = new List<byte[]>();

            int nonce = 255;
            seeds.Add(Encoding.UTF8.GetBytes("metadata"));
            seeds.Add(new PublicKey(programId).KeyBytes);
            seeds.Add(new PublicKey(mintPublicKey).KeyBytes);
            seeds.Add(new[] { (byte)nonce });

            PublicKey publicKey = null;

            while (nonce != 0)
            {
                try
                {
                    seeds[3] = new[] { (byte)nonce };
                    publicKey = CreateAddress(seeds);
                    return publicKey;
                }
                catch
                {
                    nonce--;
                }
            }

            return publicKey;
        }

        /// <summary>
        /// Returns metaplex json data from forwarded jsonUrl
        /// </summary>
        public static async Task<T> GetMetaplexJsonData<T>(string jsonUrl)
        {
            HttpClient client = new HttpClient();

            HttpResponseMessage response = await client.GetAsync(jsonUrl);
            response.EnsureSuccessStatusCode();

            try
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                T data = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(responseBody);
                client.Dispose();
                return data;
            }
            catch
            {
                client.Dispose();
                return default;
            }
        }

        
        /// <summary>
        /// Get AccountData
        /// </summary>
        /// <param name="accountPublicKey"></param>
        /// <param name="rpcClient"></param>
        /// <returns></returns>
        public static async Task<AccountInfo> GetAccountData(string accountPublicKey, IRpcClient rpcClient)
        {
            var result = await rpcClient.GetAccountInfoAsync(accountPublicKey);
            return result.Result is {Value: { }} ? result.Result.Value : null;
        }
    }
}
