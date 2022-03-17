using Solnet.Rpc.Models;
using Solnet.Wallet;
using System;
using System.Collections.Generic;
using Merkator.BitCoin;

namespace Solnet.Programs
{
    /// <summary>
    /// Implements the Associated Token Account Program methods.
    /// <remarks>
    /// For more information see: https://spl.solana.com/associated-token-account
    /// </remarks>
    /// </summary>
    public static class AssociatedTokenAccountProgram
    {
        /// <summary>
        /// The address of the Shared Memory Program.
        /// </summary>
        public static readonly PublicKey ProgramIdKey = new PublicKey("ATokenGPvbdGVxr1b2hvZbsiqW5xWH25efTNsLJA8knL");

        /// <summary>
        /// The program's name.
        /// </summary>
        private const string ProgramName = "Associated Token Account Program";

        /// <summary>
        /// The instruction's name.
        /// </summary>
        private const string InstructionName = "Create Associated Token Account";

        /// <summary>
        /// Initialize a new transaction which interacts with the Associated Token Account Program to create
        /// a new associated token account.
        /// </summary>
        /// <param name="payer">The public key of the account used to fund the associated token account.</param>
        /// <param name="owner">The public key of the owner account for the new associated token account.</param>
        /// <param name="mint">The public key of the mint for the new associated token account.</param>
        /// <returns>The transaction instruction, returns null whenever an associated token address could not be derived..</returns>
        public static TransactionInstruction CreateAssociatedTokenAccount(string payer, string owner, string mint)
        {
            PublicKey associatedTokenAddress = DeriveAssociatedTokenAccount(
                Base58Encoding.Decode(owner),
                Base58Encoding.Decode(mint)
                );

            if (associatedTokenAddress == null) return null;
            
            var keys = new List<AccountMeta>
            {
                new AccountMeta(Base58Encoding.Decode(payer), false, false),
                new AccountMeta(associatedTokenAddress.KeyBytes, false, false),
                new AccountMeta(Base58Encoding.Decode(owner), false, false),
                new AccountMeta(Base58Encoding.Decode(mint), false, true),
                new AccountMeta(Base58Encoding.Decode(SystemProgram.ProgramId), false, false),
                new AccountMeta(Base58Encoding.Decode(TokenProgram.ProgramId), false, false),
                new AccountMeta(Base58Encoding.Decode(SysVars.RentKey), false, false)
            };


            return new TransactionInstruction
            {
                ProgramId = ProgramIdKey.KeyBytes,
                Keys = keys,
                Data = Array.Empty<byte>()
            };
        }

        /// <summary>
        /// Derive the public key of the associated token account for the
        /// </summary>
        /// <param name="owner">The public key of the owner account for the new associated token account.</param>
        /// <param name="mint">The public key of the mint for the new associated token account.</param>
        /// <returns>The public key of the associated token account if it could be found, otherwise null.</returns>
        public static PublicKey DeriveAssociatedTokenAccount(byte[] owner, byte[] mint)
        {
            bool success = PublicKey.TryFindProgramAddress(
                new List<byte[]> { owner, Base58Encoding.Decode(TokenProgram.ProgramId), mint },
                ProgramIdKey, out PublicKey derivedAssociatedTokenAddress, out _);
            return derivedAssociatedTokenAddress;
        }
    }
}
