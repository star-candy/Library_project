using System;
using System.Windows.Forms;
using LibraryProject.Views.Auth;
using LibraryProject.Controllers;
using LibraryProject.Models;

namespace LibraryProject
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            //User.PrintAllUsers();
            //LoanRecord.PrintAllLoans();
            AuthController authController = new AuthController();
            authController.ShowAuthView();
        }
    }
}