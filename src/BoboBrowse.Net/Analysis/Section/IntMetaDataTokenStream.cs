// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Analysis.Section
{
    using Lucene.Net.Analysis;
    using Lucene.Net.Analysis.Tokenattributes;
    using Lucene.Net.Index;

    public class IntMetaDataTokenStream : TokenStream
    {
        private readonly string _tokenText;
        private readonly TermAttribute _termAttribute;
        private readonly OffsetAttribute _offsetAttribute;
        private readonly PayloadAttribute _payloadAtt;
        private Payload _payload;
        private bool _returnToken = false;

        public IntMetaDataTokenStream(string tokenText)
        {
            _tokenText = tokenText;
            _termAttribute = (TermAttribute)AddAttribute<TermAttribute>();
            _offsetAttribute = (OffsetAttribute)AddAttribute<OffsetAttribute>();
            _payloadAtt = (PayloadAttribute)AddAttribute<PayloadAttribute>();
        }

        /// <summary>
        /// sets meta data
        /// </summary>
        /// <param name="data">array of integer metadata indexed by section id</param>
        public virtual void SetMetaData(int[] data)
        {
            byte[] buf = new byte[data.Length * 4];
            int i = 0;

            for (int j = 0; j < data.Length; j++)
            {
                int datum = data[j];
                buf[i++] = (byte)(datum);
                buf[i++] = (byte)(((uint)datum) >> 8);
                buf[i++] = (byte)(((uint)datum) >> 16);
                buf[i++] = (byte)(((uint)datum) >> 24);
            }

            _payload = new Payload(buf);
            _returnToken = true;
        }

        public override bool IncrementToken()
        {
            if (_returnToken)
            {
                _termAttribute.SetTermBuffer(_tokenText);
                _offsetAttribute.SetOffset(0, 0);
                _payloadAtt.Payload = _payload;
                _returnToken = false;
                return true;
            }
            return false;
        }

        protected override void Dispose(bool disposing)
        {
        }
    }
}
