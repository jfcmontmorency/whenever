using UnityEngine;

namespace JFCartier.Whenever
{
    [CreateAssetMenu(menuName = "Whenever/Variables/Int Variable", fileName = "New Int Variable")]
    public sealed class IntVariable : VariableBase<int>
    {
        protected override void LoadPersisted()
        {
            RuntimeValue = PlayerPrefs.GetInt(Id, InitialValue);
        }

        protected override void SavePersisted()
        {
            PlayerPrefs.SetInt(Id, RuntimeValue);
            PlayerPrefs.Save();
        }
    }
}
