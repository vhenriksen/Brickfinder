   M S B A M L   `   `   `  ����  BrickFinder01    BrickFinder01.MainWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   2   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     6o     � Title$  Brickfinder 0.1��6	   .��5      �  � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5	      $
��Auto �6    � ColumnDefinitions 5   
   �� 5      $
��Auto �6   �� 5      .+�5   
   �� -   5        $��ButtonExtractLots��5   .   $��0��6   $��0��6   $	��100��6G   $��10q�6w   $��5q�6�   $��Stretch=�6�   $��Stretch=�6�   .��6�   Extract lots6�   �� 5   
   -   5        $!��TextBoxWantedListSourcecode��5   �   $��0��6   $��1��6   $	��100��6/   $	��100��6?   $��Stretch=�6O   $��	5,10,5,10q�6m    �� TextWrapping$
 Wrap=�6�   $	��600��6�   $	��Top=�6�    �� AcceptsReturn .  �� 
AcceptsTab . " �� VerticalScrollBarVisibility$ Visible=�6  �� 5   
   $��2��6   $��0��6"   $��0,0,0,0q�62   .+�5      �� -   5        $��ButtonSaveData��5   *   $��	Save data��6   $��5q�6@   $
��10,5q�6L   ���	#FFE818186w   �� 5      -   5        $��ButtonLoadData��5   )   $��	Load data��6   $��5q�6?   $
��10,5q�6K   ���	#FFE818186v   �� 5      -   5        $��ButtonShowWantedList��5      $��Show wantedlist��62   $��	10,5,10,5q�6L   $��5q�6_   $��Center=�6�   �� 5      .��6   Currency modifier6'   �� 5      -   5        $��TextBoxCurrencyModifier��5      .��65   1,06:   �� 5      .��6   Average Shipping costs6,   �� 5      -   5        $��TextBoxShipping��5      .��61   30,067   �� 5      .��6   	Max shops6   �� 5       -   5        $��TextBoxMaxShops��5       .��6-   860   �� 5!      .��6   Max paths per shop6(   �� 5"      -	   5        $��TextBoxMaxDeepth��5"      .��6.   561   �� 5#      -
   5        $��ButtonCombinationsToCheck��5#      $��Combinations to check?��66   6W   �� 5$      -   5        $��ButtonFindPrice��5$   *   $��
Find price��6   $��5q�6A   $
��10,5q�6M   ���	#FF8787E56y   �� 5%      -   5        $��ButtonCancel��5%   &   $��Cancel��6   $��5q�6:   $
��10,5q�6F   ���	#FF8787E56k   �� 5&      -   5        $��ButtonCopyToOutput��5&   .   $��Copy to output��6   $��5q�6H   $
��10,5q�6T   ���	#FF8787E56�   5'      �� 5*      -   5        $��TextBoxInfo��5*   5    �� 
IsReadOnly . $	��420��6(   $��1��6H   $��2��6X   $��5,5,5,5q�6e   $ Visible=�6v   $��Stretch=�6�   6�   �� 5-   
   -   5        $��ButtonQueryShops��5-   f   $��Query shops for inventory��6   $��0��65   $��1��6E   $��1��6R   $��10q�6�   $��5q�6�   $��Center=�6�   �� 5.   
   $��1��6   $��1��6%    �� Orientation$ 
Horizontal=�62   .+�5/   
   �� -   5        $��CheckBoxLimitDataToEU��5/   p   $��Limit data to EU shops��6   $��16��66   $
��Left=�6B   $��	10,10,0,0q�6]   6p   �� 50      $��20��6   6!   �� 51      $��25��6   .��6    Ship country ID:63   �� 52      -   5        $��TextBoxShipCountryId��52      $	��100��62   $��Stretch=�6A   $��25��6_   .��6k   DK6o   53      54      55      