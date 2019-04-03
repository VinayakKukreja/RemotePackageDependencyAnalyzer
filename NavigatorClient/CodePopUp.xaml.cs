//////////////////////////////////////////////////////////////////////////////////
// CodePopUp.xaml.cs - Displays text file source in response to double-click   //
// ver 1.0                                                                    //
// Source : Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2017  //
// Author: Vinayak Kukreja                                                  //
/////////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * Used to display file as a txt when double clicked on it.
 *  
 *
 * Required Files:
 * -----------------
 *   CodePopUp.xaml, CodePopUp.xaml.cs, MainWindow.xaml, MainWindow.xaml.cs
 *   
 *
 * Public Interface :
 * -------------------
 * Class CodePopUp:
 * public CodePopUp(): Used to initalize the component.
 *
 *
 *
 * Maintenance History:
 * --------------------
 *
 * Author:
 *  ver 1.0 : 1 DEC 2018
 * - first release
 *
 */



using System.Windows;

namespace Navigator
{
    public partial class CodePopUp : Window
    {
        public CodePopUp()
        {
            InitializeComponent();
        }
    }
}