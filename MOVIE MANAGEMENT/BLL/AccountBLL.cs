﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;
using EmailValidation;
using System.Text.RegularExpressions;
using System.Net.Mail;
using System.Security.Cryptography;
using DAL;
using DTO;

namespace BLL
{
    public class AccountBLL
    {
        private static AccountBLL _instance;
        public static AccountBLL Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new AccountBLL();
                return _instance;
            }
            set { }
        }
        private static readonly string _from = "g8cinestar.@gmail.com"; // Email của Sender (của bạn)
        private static readonly string _pass = "quanlirapchieuphim"; // Mật khẩu Email của Sender (của bạn)
        private static string content;
        //private static string email_reset;
        public string email_employee { get; set; }
        public bool CheckUsernamePass(string un)
        {
            return Regex.IsMatch(un, "^[a-zA-Z0-9]{10,24}$");
        }
        public bool CheckEmail(string em)
        {
            return Regex.IsMatch(em, @"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
        }
        public bool CheckRole(string username)
        {
            return AccountDAL.Instance.CheckRole(username);
        }
        public bool CheckChangePass(string username)
        {
            return AccountDAL.Instance.CheckChangePass(username);
        }
        public bool CheckPhoneNumber(string pn)
        {
            return Regex.IsMatch(pn, "^[0-9]{10}$");
        }
        public bool CheckEmailExist(string em)
        {
            EmailValidator emailValidator = new EmailValidator();
            EmailValidationResult result;

            emailValidator.Validate(em, out result);
            switch (result)
            {
                case EmailValidationResult.OK:
                    return true;
                case EmailValidationResult.MailboxUnavailable:
                    return false;
                default:
                    return false;
            }
        }

        public string CheckAccount(Account account)
        {
            if (account.Fullname == "")             return "Invalid Fullname. Please enter your fullname."; 
            if (!CheckUsernamePass(account.Username))     return "Invalid Username! Minimum 10 characters.\nMust not contain symbols."; 
            if (!CheckPhoneNumber(account.Phone))    return "Invalid phone number! Your phone number.\nmust contain 10 characters."; 
            if (!CheckEmail(account.Email))  return "Invalid Email!\nThe format of the email address is:username@gmail.com.\nUsername must not contain symbols";
            if (!CheckEmailExist(account.Email)) return "Email address does not exist!";
            return "OK";
        }
        private int RandomNumber(int min, int max)
        {
            Random random = new Random();
            return random.Next(min, max);
        }
        private string RandomString(int size, bool lowerCase)
        {
            StringBuilder builder = new StringBuilder();
            Random random = new Random();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }
            if (lowerCase)
                return builder.ToString().ToLower();
            return builder.ToString();
        }
        public string RandomPassword()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(RandomString(4, true));
            builder.Append(RandomNumber(1000, 9999));
            builder.Append(RandomString(2, false));
            return builder.ToString();
        }
        public string HashPass(string password)
        {
            SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();
            byte[] password_bytes = Encoding.ASCII.GetBytes(password);
            byte[] encrypted_bytes = sha1.ComputeHash(password_bytes);
            return Convert.ToBase64String(encrypted_bytes);
        }
        public string Add(Account account)
        {
            string check = CheckAccount(account);
            if (check != "OK") return check;

            string pwd = RandomPassword() ;
            account.Password = HashPass(pwd);

            check = AccountDAL.Instance.Add(account);
            if(check == "OK")
            {
                email_employee = account.Email;
                content = "Welcome to CINESTAR CINEMA.<br>We create a new account for you.<br>Username: "+account.Username+"<br>Password: "+pwd;
                SendEmail(email_employee,content,"YOUR NEW ACCOUNT AT CINESTAR CINEMA.");
            }
            return check;
        }
        public string GetEmailByUsername(string username)
        {
            email_employee = AccountDAL.Instance.GetEmailByUsername(username);
            return email_employee;
        }
        public string Update(Account account)
        {
            string check = CheckAccount(account);
            if (check != "OK") return check;
            return AccountDAL.Instance.Update(account);
        }
        public DataTable LoadAllAccount()
        {
            return AccountDAL.Instance.LoadAllAccount();
        }
        public DataTable LoadSearchAccount(string txt)
        {
            return AccountDAL.Instance.LoadSearchAccount(txt);
        }
        public DataRow LoadAccountByID(int id)
        {
            return AccountDAL.Instance.LoadAccountByID(id);
        }
        public bool CheckLogin(string username, string password)
        {
            if(!(CheckUsernamePass(username) && CheckUsernamePass(password))) return false;
            return AccountDAL.Instance.CheckLogin(username, HashPass(password));
        }
        public string CheckAndSendMailToReset(string email)
        {
            if (!CheckEmail(email)) return "Invalid Email! Email address is not registered!";
            string check = AccountDAL.Instance.CheckMailToReset(email);
            if (check == "OK")
            {
                email_employee = email;
                content = RandomNumber(99999, 1000000).ToString();
                SendEmail(email_employee,content,"RESET PASSWORD");
            }
            return check;
        }
        public void CheckAndSendMailToFirstLogin(string username)
        {
            content = RandomNumber(99999, 1000000).ToString();
            SendEmail(GetEmailByUsername(username), content, "FIRST LOGIN");
        }
        public void SendEmail(string email, string content, string subject)
        {
            MailMessage mail = new MailMessage();
            SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");
            
            mail.From = new MailAddress(_from);
            mail.To.Add(email_employee);
            mail.IsBodyHtml = true;
            mail.Body = content;
            mail.Subject = subject;
            mail.Priority = MailPriority.High;
            SmtpServer.Port = 587;
            SmtpServer.Credentials = new System.Net.NetworkCredential(_from, _pass);
            SmtpServer.EnableSsl = true;

            SmtpServer.Send(mail);
        }
        
        public bool CheckOTP(string otp)
        {
            if (otp == content) return true;
            return false;
        }
        public string ResetPass(string newpass, string confirmpass)
        {
            string check;
            if (CheckUsernamePass(newpass))
            {
                if(confirmpass == newpass)
                { 
                    AccountDAL.Instance.ResetPass(HashPass(newpass), email_employee);
                    check = "OK";
                }
                else check = "Your confirm password doesn't match your new password";

            }
            else check = "Invalid Password! Minimum 10 characters.\r\nMust not contain symbols.";
            return check;
        }
        public void Delete(List<int> id)
        {
            foreach(int i in id)
            {
                AccountDAL.Instance.Delete(i);
            }
        }
    }
}
