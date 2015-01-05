// Version compatibility level: 3.2.0
namespace BoboBrowse.Net
{
    public interface IFieldValueAccessor
    {
        string GetFormatedValue(int index);
        object GetRawValue(int index);
    }
}
