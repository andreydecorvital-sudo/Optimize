// SysManager · TimerResolutionView
// Author: laurentiu021 · https://github.com/laurentiu021/SystemManager
// License: MIT

using System.Windows;
using System.Windows.Controls;
using SysManager.Helpers;

namespace SysManager.Views;

public partial class TimerResolutionView : UserControl
{
    public TimerResolutionView()
    {
        InitializeComponent();
        Loaded += (_, _) => LegacyOptimizationLock.Apply(this,
            "O ajuste isolado de resolução do temporizador está bloqueado. Use o Perfil para jogos, que aplica e reverte esse ajuste somente durante a sessão quando for compatível.");
    }
}
