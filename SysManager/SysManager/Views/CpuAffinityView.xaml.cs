// SysManager · CpuAffinityView
// Author: laurentiu021 · https://github.com/laurentiu021/SystemManager
// License: MIT

using System.Windows;
using System.Windows.Controls;
using SysManager.Helpers;

namespace SysManager.Views;

public partial class CpuAffinityView : UserControl
{
    public CpuAffinityView()
    {
        InitializeComponent();
        Loaded += (_, _) => LegacyOptimizationLock.Apply(this,
            "A afinidade manual de CPU está bloqueada até a topologia deste processador ser validada. Use o Perfil para jogos para ajustes temporários, por processo e reversíveis.");
    }
}
