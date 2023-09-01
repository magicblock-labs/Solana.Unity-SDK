using Solana.Unity.Metaplex.CandyGuard;
using Solana.Unity.Programs.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Solana.Unity.SDK.Metaplex
{
    public partial class GuardData
    {

        #region Constants

        private const int MAX_LABEL_LENGTH = 6;
        private const int ACCOUNT_DATA_OFFSET =
            8     //     8 (discriminator)
            + 32  //  + 32 (base)
            + 1   //  +  1 (bump)
            + 32; //  + 32 (authority)

        #endregion

        #region Properties

        public GuardSet Default { get; set; }

        public Group[] Groups { get; set; }

        #endregion

        #region Public

        public int Serialize(byte[] _data, int initialOffset)
        {
            int offset = initialOffset;
            offset += Default.Serialize(_data, offset);
            var groupCounter = (uint)(Groups?.Length ?? 0);
            _data.WriteU32(groupCounter, offset);
            offset += 4;
            if (Groups != null) {
                foreach (var group in Groups) {
                    if (group.Label.Length > MAX_LABEL_LENGTH) {
                        throw new InvalidOperationException("Guard group labels must be less than 6 characters in length.");
                    }
                    var labelBytes = Encoding.UTF8.GetBytes(group.Label);
                    _data.WriteSpan(labelBytes, offset);
                    offset += MAX_LABEL_LENGTH;
                    offset += group.Guards.Serialize(_data, offset);
                }
            }
            return offset;
        }

        public static GuardData Deserialize(ReadOnlySpan<byte> _data, int initialOffset)
        {
            var result = new GuardData();
            var offset = initialOffset + ACCOUNT_DATA_OFFSET;
            offset += GuardSet.Deserialize(_data, offset, out var defaultSet);
            result.Default = defaultSet;
            var groupCount = _data.GetU32(offset);
            offset += 4;
            var groups = new List<Group>();
            for (int i = 0; i < groupCount; i++) 
            {
                var labelBytes = _data.GetSpan(offset, MAX_LABEL_LENGTH);
                var label = Encoding.UTF8.GetString(labelBytes);
                offset += MAX_LABEL_LENGTH;
                offset += GuardSet.Deserialize(_data, offset, out var guardGroup);
                groups.Add(new() {
                    Guards = guardGroup,
                    Label = label
                });
            }
            result.Groups = groups.ToArray();
            return result;
        }

        #endregion
    }
}
