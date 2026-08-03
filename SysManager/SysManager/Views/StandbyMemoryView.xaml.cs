// SysManager · StandbyMemoryView
// Author: laurentiu021 · https://github.com/laurentiu021/SystemManager
// License: MIT

using System.Windows;
using System.Windows.Controls;
using SysManager.Helpers;

namespace SysManager.Views;

public partial class StandbyMemoryView : UserControl
{
    public StandbyMemoryView()
    {
        InitializeComponent();
        Loaded += (_, _) => LegacyOptimizationLock.Apply(this,
            "A limpeza automática/isolada de memória em espera está bloqueada. O Optimize só permite esse ajuste de forma pontual e contextual, sem vender 'limpeza de RAM' como ganho permanente.");
    }
}
