using Solana.Unity.Metaplex.CandyGuard;
using Solana.Unity.Programs.Utilities;
using System;
using System.Text;

namespace Solana.Unity.SDK.Metaplex
{
    public partial class GuardData
    {

        #region Constants

        private const int MAX_LABEL_LENGTH = 6;

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

        #endregion
    }
}
