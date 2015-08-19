using System.ComponentModel;

namespace CsEnumsToJsTestTarget
{
    [JsEnum]
    public enum EnumWithDescriptions
    {
        MultiWordEnumValue,
        [Description("More complex description!!1!")] MoreComplexValue
    }
}