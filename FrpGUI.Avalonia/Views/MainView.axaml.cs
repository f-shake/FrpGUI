using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Messaging;
using FrpGUI.Avalonia.ViewModels;
using FrpGUI.Models;
using FzLib.Avalonia.Controls;
using FzLib.Avalonia.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace FrpGUI.Avalonia.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        App.Services.GetRequiredService<IProgressOverlayService>().Attach(ring);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        //不知道为什么，Linux无法找到TopLevel，因此强制指定
        App.Services.GetRequiredService<IDialogService>().DefaultOwner =
            TopLevel.GetTopLevel(this) ?? throw new InvalidOperationException("找不到当前的顶层");
    }
}