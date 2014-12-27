namespace BoboBrowse.Net.Service
{
    using System.IO;
    using BoboBrowse.Net;
    using Common.Logging;
    using Lucene.Net.Index;
    using Lucene.Net.Store;

    public class BoboService
    {
        private static ILog logger = LogManager.GetLogger(typeof(BoboService));

        private readonly DirectoryInfo idxDir;
        private BoboIndexReader boboReader;

        public BoboService(string path)
            : this(new DirectoryInfo(path))
        {
        }

        public BoboService(DirectoryInfo idxDir)
        {
            this.idxDir = idxDir;
            boboReader = null;
        }

        public virtual BrowseResult Browse(BrowseRequest req)
        {
            BoboBrowser browser = null;
            try
            {
                browser = new BoboBrowser(boboReader);
                return browser.Browse(req);
            }
            catch (BrowseException be)
            {
                logger.Error(be.Message, be);
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
                    catch (IOException e)
                    {
                        logger.Error(e.Message);
                    }
                }
            }
        }

        public virtual void Start()
        {
            IndexReader reader = IndexReader.Open(FSDirectory.Open(idxDir), true);
            try
            {
                boboReader = BoboIndexReader.GetInstance(reader);
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
            if (boboReader != null)
            {
                try
                {
                    boboReader.Dispose();
                }
                catch (IOException e)
                {
                    logger.Error(e.Message);
                }
            }
        }
    }
}