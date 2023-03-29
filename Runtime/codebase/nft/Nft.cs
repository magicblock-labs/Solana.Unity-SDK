using Solana.Unity.SDK.Utility;
using Solana.Unity.Rpc;
using System;
using System.IO;
using System.Threading.Tasks;
using Solana.Unity.Metaplex.NFT.Library;
using Solana.Unity.Rpc.Types;
using UnityEngine;
using Solana.Unity.Wallet;

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
        /// <param name="commitment"></param>
        /// <returns></returns>
        public static async Task<Nft> TryGetNftData(
            string mint,
            IRpcClient connection, 
            bool tryUseLocalContent = true,
            Commitment commitment = Commitment.Confirmed)
        {
            if (tryUseLocalContent)
            { 
                var nft = TryLoadNftFromLocal(mint);
                if (nft != null)
                {
                    return nft;
                }
            }
            var newData = await MetadataAccount.GetAccount( connection, new PublicKey(mint), commitment);
            
            if (newData?.metadata == null || newData?.offchainData == null) return null;
            var met = new Metaplex(newData);

            var nftImage = new NftImage();
            if (newData.offchainData != null)
            {
                var texture = await FileLoader.LoadFile<Texture2D>(newData.offchainData.default_image);
                var compressedTexture = FileLoader.Resize(texture, 256, 256);
                FileLoader.SaveToPersistentDataPath(Path.Combine(Application.persistentDataPath, $"{mint}.png"), compressedTexture);
                if (compressedTexture)
                {
                    nftImage.externalUrl = newData.offchainData.default_image;
                    //nftImage.file = Resize(texture, nftImage.heightAndWidth, nftImage.heightAndWidth);
                    nftImage.file = compressedTexture;
                    met.nftImage = nftImage;
                }
            }
            var newNft = new Nft(met);
            FileLoader.SaveToPersistentDataPath(Path.Combine(Application.persistentDataPath, $"{mint}.json"), newNft.metaplexData.data);
            return newNft;
        }

        /// <summary>
        /// Returns Nft from local machine if it exists
        /// </summary>
        /// <param name="mint"></param>
        /// <returns></returns>
        public static Nft TryLoadNftFromLocal(string mint)
        {
            var metadataAccount = FileLoader.LoadFileFromLocalPath<MetadataAccount>($"{Path.Combine(Application.persistentDataPath, mint)}.json");
            if (metadataAccount == null) return null;
            
            var local = new Nft(new Metaplex(metadataAccount));
            
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
    }
}
