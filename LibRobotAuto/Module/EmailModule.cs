﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Diagnostics;
using System.Threading;

namespace LibRobotAuto.Module
{
    public class EmailModule
    {
        public static string Server = "lib.whu.edu.cn";
        public static int Port = 25;
        public static string Username = "scan@lib.whu.edu.cn";
        public static string Password = "2018rfid";
        public static bool EnableSsl = false;
        public static string Subject = "机器人卡死";
        public static string Body = "机器人卡死!";
        public static string SendName = "TOOKER";
        public static string[] To = new string[] { "netlab624@163.com" };
        public static string[] ToWHU = new string[] { "liquan@lib.whu.edu.cn",
            "hqh@lib.whu.edu.cn", "whduan@lib.whu.edu.cn",
            "longquan@lib.whu.edu.cn", "xuqiong@lib.whu.edu.cn" };

        private static int TRY_SEND = 3;

        private MailMessage Mail { get; set; }
        private SmtpClient Host { get; set; }

        public EmailModule()
        {
            //Host config
            Host = new SmtpClient(Server, Port);
            Host.Credentials = new System.Net.NetworkCredential(Username, Password);
            Host.EnableSsl = EnableSsl;
            Host.DeliveryMethod = SmtpDeliveryMethod.Network;
        }
        public bool sendEmail(string no)
        {
            Mail = new MailMessage();
            Mail.From = new MailAddress(Username, SendName);

            foreach (var t in To)
                Mail.To.Add(t);

            Mail.Subject = Subject;
            Mail.Body = no + "号" + Body;
            Mail.IsBodyHtml = true;
            Mail.BodyEncoding = System.Text.Encoding.UTF8;
            Mail.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
            try
            {
                Host.Send(Mail);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                return false;
            }
            return true;
        }
        public bool sendEmail()
        {
            Mail = new MailMessage();
            Mail.From = new MailAddress(Username, SendName);

            foreach (var t in To)
                Mail.To.Add(t);

            Mail.Subject = Subject;
            Mail.Body = Body;
            Mail.IsBodyHtml = true;
            Mail.BodyEncoding = System.Text.Encoding.UTF8;
            Mail.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
            try
            {
                Host.Send(Mail);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                return false;
            }
            return true;
        }
        public bool sendEmail(string subject, string body, bool toWHU = false)
        {
            Mail = new MailMessage();
            Mail.From = new MailAddress(Username, SendName);

            foreach (var t in To)
            {
                Mail.To.Add(t);
            }
            if (toWHU)
            {
                foreach (var t in ToWHU)
                {
                    Mail.To.Add(t);
                }
            }

            Mail.Subject = subject;
            Mail.Body = body;
            Mail.IsBodyHtml = true;
            Mail.BodyEncoding = System.Text.Encoding.UTF8;
            Mail.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
            try
            {
                Host.Send(Mail);
            }
            catch (Exception e)
            {
                Mail.Dispose();
                Trace.TraceError(e.ToString());
                return false;
            }
            Mail.Dispose();
            return true;
        }
        public bool sendEmail(string subject, string body, string[] to)
        {
            Mail = new MailMessage();
            Mail.From = new MailAddress(Username, SendName);

            foreach (var t in to)
                Mail.To.Add(t);

            Mail.Subject = subject;
            Mail.Body = body;
            Mail.IsBodyHtml = true;
            Mail.BodyEncoding = System.Text.Encoding.UTF8;
            Mail.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
            try
            {
                Host.Send(Mail);
            }
            catch (Exception e)
            {
                Mail.Dispose();
                Trace.TraceError(e.ToString());
                return false;
            }
            Mail.Dispose();
            return true;
        }
        /**
         * 发送邮件，会尝试多次
         * subject: 邮件主题
         * body:    邮件内容
         * toWHU:   是否发送给武大
         */
        public void trySendEmail(string subject, string body, bool toWHU=false)
        {
            int tryCount = 0;
            while (!sendEmail(subject, body, toWHU))
            {
                Trace.TraceInformation("邮件发送失败，正在重新发送！");
                //发送失败，暂停五秒尝试重发
                Thread.Sleep(5000);
                tryCount++;
                if (tryCount > TRY_SEND)
                {
                    break;
                }
            }
        }
        //public static void Main(string[] args)
        //{
        //    //方法体
        //    EmailModule email = new EmailModule();
        //    email.trySendEmail("Test", "这是一封测试邮件，请无视", true);
        //}
    }
}