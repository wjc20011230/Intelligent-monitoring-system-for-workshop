   M S B A M L   `   `   `  ����I  EProductMonitor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null;"clr-namespace:ProductMonitor.ViewsProductMonitor.Views  5   	   %    ProductMonitor.Views.SettingWin   5   	   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   4local"clr-namespace:ProductMonitor.Views     5   	     � WindowStartupLocation$  CenterOwner=�5   	   
������ $��Microsoft YaHei)�6F   $��
ExtraLight"�6c    � Title$ 系统配置��5	   	   $	��350��6   $	��600��6#    � 	Resources 5
       �� 
TargetType �� Property �� Value �� 
TargetType �� Triggers P� Property	 P� Value��%�  '	�       �� 5   
   " 	�6   .�5      ��  � ��
 ���� 6A   �� 5       � �� 5      �� 5      " 	�6&   .R�5      �� #��z��6!   $��30��6K   ._�5      �� $��Center=�6/   $��Center=�6J   $	��5,0q�6g   6v   5      5      5       5      P� 5       �@d�	IsChecked$
	 TrueY�6/   . �5      ��  � �� �#18aabd6E   �� 5       � ��
 �����6C   5      5      5      '  !System.Windows.Shell.WindowChrome
   WindowChrome
 5        5   
      GlassFrameThickness$ -1q�6   62   .��5!      �  � RowDefinitions 5"   
   �� 5#      $��30 �6   6*   �� 5$      6    � ColumnDefinitions 5&   
   �� 5'      $	��100 �6   6-   �� 5(      6!   .+�5*   
   ��  �� Orientation$ 
Horizontal=�6   $��Center=�6.   .+�5+   
   �� $	����6   $!��../Resource/Fonts/#iconfont)�6$   $��Center=�6M   $
��Left=�6h   $	��4,0q�6�   6�   �� 5,   
   $��系统配置��6   $��Center=�6    $��0,0,0,0q�6;   6N   5-      �� 5.   
   $��0,0,1,0q�6   $��1��6+   6:   �� 5/   
   $��1��6   $��0��6"   $
��10,0q�62   .+�50      	� $��设备清单��6    d� 	IsChecked$
 TrueY�6)   6<   	� 51      $��环境监测��6   6+   	� 52      $��设备监测��6   6+   	� 53      $��	配置一��6   6*   	� 54      $��	配置二��6   6*   55      �� 56   
   $��1��6   $��1��6$   $��Hidden=�64   .��57       �   � Source$ SettingPage1.xaml?�6     � NavigationUIVisibility$ Hidden=�6/   6Q   58      59      5:      