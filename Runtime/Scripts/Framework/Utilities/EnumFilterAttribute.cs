using UnityEngine;

public class EnumFilterAttribute : PropertyAttribute {
    public readonly object[] displayedOptions;
    public readonly string[] optionsString;

    public EnumFilterAttribute(params object[] displayedOptions) {
        this.displayedOptions = displayedOptions;
        this.optionsString = new string[displayedOptions.Length];

        for(int i=0; i<displayedOptions.Length; i++) {
            this.optionsString[i] = displayedOptions[i].ToString();
        }
    }
}