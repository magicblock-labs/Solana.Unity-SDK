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
        ///         /// <param name="loadTexture"></param>

        /// <param name="imageHeightAndWidth"></param>
        /// <param name="tryUseLocalContent">If use local content for image</param>
        /// <param name="commitment"></param>
        /// <returns></returns>
        public static async Task<Nft> TryGetNftData(
            string mint,
            IRpcClient connection, 
            bool loadTexture = true,
            int imageHeightAndWidth = 256,
            bool tryUseLocalContent = true,
            Commitment commitment = Commitment.Confirmed)
        {
            if (tryUseLocalContent)
            { 
                var nft = TryLoadNftFromLocal(mint);
                if(nft != null && loadTexture) await nft.LoadTexture();
                if (nft != null) return nft;
            }
            var newData = await MetadataAccount.GetAccount( connection, new PublicKey(mint), commitment);
            
            if (newData?.metadata == null || newData?.offchainData == null) return null;

            var met = new Metaplex(newData);
            var newNft = new Nft(met);

            if (loadTexture) await newNft.LoadTexture(imageHeightAndWidth);
            
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
        
        /// <summary>
        /// Load the texture of the NFT
        /// </summary>
        /// <param name="imageHeightAndWidth"></param>
        public async Task LoadTexture(int imageHeightAndWidth = 256)
        {
            if (metaplexData.data.offchainData == null) return;
            if (metaplexData.nftImage != null) return;
            var nftImage = new NftImage();
            var texture = await FileLoader.LoadFile<Texture2D>(metaplexData.data.offchainData.default_image);
            if (texture == null)
            {
                Debug.LogWarning($"Unable to load: {metaplexData?.data?.offchainData?.default_image}");
                return;
            }
            var compressedTexture = FileLoader.Resize(texture, imageHeightAndWidth, imageHeightAndWidth);
            if (compressedTexture)
            {
                nftImage.externalUrl = metaplexData.data.offchainData.default_image;
                nftImage.file = compressedTexture;
                metaplexData.nftImage = nftImage;
                nftImage.externalUrl = metaplexData.data.offchainData.default_image;
            }
            FileLoader.SaveToPersistentDataPath(Path.Combine(Application.persistentDataPath, $"{metaplexData.data.mint}.png"), compressedTexture);
        }
    }
}
