// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Service
{
    using BoboBrowse.Net;
    using Common.Logging;
    using Lucene.Net.Index;
    using Lucene.Net.Store;
    using System;
    using System.IO;

    public class BoboService
    {
        private static ILog logger = LogManager.GetLogger(typeof(BoboService));

        private readonly DirectoryInfo _idxDir;
        private BoboIndexReader _boboReader;

        public BoboService(string path)
            : this(new DirectoryInfo(path))
        {
        }

        public BoboService(DirectoryInfo idxDir)
        {
            this._idxDir = idxDir;
            _boboReader = null;
        }

        public virtual BrowseResult Browse(BrowseRequest req)
        {
            BoboBrowser browser = null;
            try
            {
                browser = new BoboBrowser(_boboReader);
                return browser.Browse(req);
            }
            catch (Exception e)
            {
                logger.Error(e.Message, e);
                return new BrowseResult();
            }
            finally
            {
                if (browser != null)
                {
                    try
                    {
                        browser.Dispose();
                    }
                    catch (Exception e)
                    {
                        logger.Error(e.Message);
                    }
                }
            }
        }

        public virtual void Start()
        {
            IndexReader reader = IndexReader.Open(FSDirectory.Open(_idxDir), true);
            try
            {
                _boboReader = BoboIndexReader.GetInstance(reader);
            }
            catch
            {
                if (reader != null)
                {
                    reader.Dispose();
                }
            }
        }

        public virtual void Shutdown()
        {
            if (_boboReader != null)
            {
                try
                {
                    _boboReader.Dispose();
                }
                catch (Exception e)
                {
                    logger.Error(e.Message);
                }
            }
        }
    }
}