
namespace BoboBrowse.Net
{
    using System;

    public interface IFieldValueAccessor
    {
        string GetFormatedValue(int index);
        object GetRawValue(int index);
    }
}
