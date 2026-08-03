// SysManager · TweaksHubView
// Author: laurentiu021 · https://github.com/laurentiu021/SystemManager
// License: MIT

using System.Windows;
using System.Windows.Controls;
using SysManager.Helpers;

namespace SysManager.Views;

public partial class TweaksHubView : UserControl
{
    public TweaksHubView()
    {
        InitializeComponent();
        Loaded += (_, _) => LegacyOptimizationLock.Apply(this,
            "A Central de ajustes herdada está em modo somente leitura. Cada tweak será liberado individualmente quando tiver compatibilidade, risco, backup e reversão auditados pelo Optimize.");
    }
}
