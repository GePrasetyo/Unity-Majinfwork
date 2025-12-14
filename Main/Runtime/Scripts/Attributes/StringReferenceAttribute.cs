using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]
public class StringReferenceAttribute : PropertyAttribute {
    public Type classType;
    public string propertyName;

    public StringReferenceAttribute(Type classType, string propertyName) {
        this.classType = classType;
        this.propertyName = propertyName;
    }

    public string[] GetOptions() {
        var optionsGetter = classType.GetField(propertyName);
        return (string[])optionsGetter.GetValue(classType);
    }
}
