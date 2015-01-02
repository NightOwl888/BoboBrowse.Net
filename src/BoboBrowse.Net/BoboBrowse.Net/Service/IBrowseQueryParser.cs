// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Service
{
    using Lucene.Net.Search;

    ///<summary>Builds a DocSet from an array of SelectioNodes </summary>
    public interface IBrowseQueryParser
    {
        DocIdSet Parse(SelectionNode[] selectionNodes, SelectionNode[] notSelectionNodes, int maxDoc);
    }

    public class SelectionNode
    {
        public SelectionNode()
        {
        }

        public SelectionNode(string fieldName, DocIdSet docSet)
        {
            FieldName = fieldName;
            DocSet = docSet;
        }

        public virtual string FieldName { get; set; }
        public virtual DocIdSet DocSet { get; set; }
    }
}