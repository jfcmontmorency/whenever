using UnityEngine;

namespace JFCartier.Whenever
{
    [CreateAssetMenu(menuName = "Whenever/Variables/Float Variable", fileName = "New Float Variable")]
    public sealed class FloatVariable : VariableBase<float>
    {
        protected override void LoadPersisted()
        {
            RuntimeValue = PlayerPrefs.GetFloat(Id, InitialValue);
        }

        protected override void SavePersisted()
        {
            PlayerPrefs.SetFloat(Id, RuntimeValue);
            PlayerPrefs.Save();
        }
    }
}
