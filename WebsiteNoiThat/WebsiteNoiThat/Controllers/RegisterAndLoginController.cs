using BotDetect.Web.Mvc;
using GoogleAuthentication.Services;
using GoogleAuthentication.Services;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Google;
using Models.DAO;
using Models.EF;
using reCAPTCHA.MVC;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Security;
using System.Xml.Linq;
using WebsiteNoiThat.Areas.Admin.Models;
using WebsiteNoiThat.Common;
using WebsiteNoiThat.Models;
using Facebook;
namespace WebsiteNoiThat.Controllers
{
    public class RegisterAndLoginController : Controller
    {
        // GET: RegisterAndLogin
        DBNoiThat db = new DBNoiThat();
        private IAuthenticationManager Authentication => HttpContext.GetOwinContext().Authentication;
        public ActionResult Logout()
        {
            Session[Commoncontent.user_sesion] = null;
            Session[Commoncontent.CartSession] = null;
            return Redirect("/");
        }
        // Gọi Google Login
        [HttpGet]
        public ActionResult Login()
        {
            return PartialView();
        }
        [HttpPost]
        public ActionResult Login(Models.LoginModel model)
        {
            if (ModelState.IsValid)
            {
                var dao = new UserDao();
                //var result = dao.Login(model.UserName, model.Password);
                var result = dao.Login(model.UserName, Encryptor.MD5Hash(model.Password));

                if (result == 1)
                {
                    var user = dao.GetById(model.UserName);

                    // Chỉ cho phép USER truy cập trang khách
                    if (user.GroupId != "USER")
                    {
                        ModelState.AddModelError("", "Tài khoản không được phép đăng nhập tại đây");
                        return View(model);
                    }

                    var userSession = new UserLogin();
                    userSession.Username = user.Username;
                    userSession.UserId = user.UserId;
                    Session.Add(Commoncontent.user_sesion, userSession);
                    return Redirect("/");
                }

                else if (result == -1)
                {
                    ModelState.AddModelError("", "Tài Khoản đang bị khóa, liên hệ admin");

                }
                else if (result == 0)
                {
                    ModelState.AddModelError("", "Tài khoản không tồn tại");
                }
                else if (result == -2)
                {
                    ModelState.AddModelError("", "Mật khẩu không đúng");
                }
            }
            return View(model);
        }
        

        public ActionResult Register()
        {
            return PartialView();
        }

        [HttpPost]
        public ActionResult Register(RegisterModel model)
        {
            if(ModelState.IsValid)
            {
                var dao = new UserDao();
                if (dao.CheckUserName(model.UserName))
                {
                    ModelState.AddModelError("", "Tên đăng nhập đã tồn tại");
                    if (dao.CheckEmail(model.Email))
                    {
                        ModelState.AddModelError("", "Email đã tồn tại");
                    }
                    if(dao.CheckPhone(model.Phone))
                    {
                        ModelState.AddModelError("", "Số điện thoại đã tồn tại");
                    }    
                }
                //else if (dao.CheckEmail(model.Email))
                //{
                //    ModelState.AddModelError("", "Email đã tồn tại");
                //}
                else
                {
                    var user = new User();
                    user.Username = model.UserName;
                    user.Password = Encryptor.MD5Hash(model.Password);
                    user.Phone = model.Phone;
                    user.Email = model.Email;
                    user.Address = model.Address;
                    user.Name = model.Name;
                    user.GroupId = "USER";

                    user.Status = true;

                    var result = dao.Insert(user);
                    if (result > 0)
                    {
                        ViewBag.Success = "Đăng ký thành công";
                        var models = db.Users.SingleOrDefault(n => n.Username == model.UserName);
                        //return RedirectToAction("Card", new { UserId= models.UserId });

                    }
                    else
                    {
                        ModelState.AddModelError("", "Đăng ký không thành công.");
                    }
                }
            }
            model = new RegisterModel();
            return View();
        }

        [HttpGet]
        public ActionResult ViewCurentUser()
        {
            var session = (UserLogin)Session[WebsiteNoiThat.Common.Commoncontent.user_sesion];
            if (session != null)
            {
                var model = db.Users.SingleOrDefault(n => n.UserId == session.UserId);
                return View(model);
            }
            else
            {
                return Redirect("/RegisterAndLogin/Login");
            }
        }

        [HttpGet]
        public ActionResult EditCurentUser()
        {
            var session = (UserLogin)Session[WebsiteNoiThat.Common.Commoncontent.user_sesion];
            var model = db.Users.SingleOrDefault(n => n.UserId == session.UserId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditCurentUser([Bind(Include = "UserId,Name,Address,Phone,Username,Password,Email,GroupId,Status")] User user)
        {
            if (ModelState.IsValid)
            {
                user.Password = Encryptor.MD5Hash(user.Password);
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("ViewCurentUser");
            }
            return View(user);
        }

        [HttpGet]
        public ActionResult Card(int UserId)

        {
           
            var session = (UserLogin)Session[WebsiteNoiThat.Common.Commoncontent.user_sesion];
            if(session!=null)
            {
                var checkuser = db.Cards.SingleOrDefault(n => n.UserId == session.UserId);
                if (checkuser == null)
                {
                    var m = db.Users.SingleOrDefault(n => n.UserId == UserId);
                    if (m != null)
                    {
                        var model = new Card();
                        model.UserId = session.UserId;
                        model.NumberCard = 0;
                        model.UserNumber = 0;
                        return View(model);

                    }
                    else
                    {
                        //var model = new Card();
                        //model.Username = session.Username;
                        //model.NumberCard = 0;
                        //model.UseNumber = 0;
                        var model = new Card();
                        model.UserId = session.UserId;
                        model.NumberCard = 0;
                        model.UserNumber = 0;
                        
                        return View(model);
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Đã có thẻ tích điểm. Bạn không thể đăng ký thêm.");
                    return View();
                }
            }
            else
            {
                var model = new Card();
                model.UserId = UserId;
                model.NumberCard = 0;
                model.UserNumber = 0;
                return View(model);
            }
           
           
        }
        [HttpPost]
        public ActionResult Card(Card n)
        {
            var model =new Card();
            model.UserId = n.UserId;
            model.NumberCard = 0;
            model.UserNumber = 0;
            model.Identification = n.Identification;

            db.Cards.Add(model);
            db.SaveChanges();
            ViewBag.Success = "Đăng ký thẻ thành công";
            return Redirect("/");
        }
        
        public ActionResult ViewLogin()
        {
            var session = (UserLogin)Session[WebsiteNoiThat.Common.Commoncontent.user_sesion];
           if(session!=null)
            {
               
                var model = db.Cards.FirstOrDefault(n => n.UserId == session.UserId);

                var models = (from a in db.OrderDetails
                            join b in db.Orders
                            on a.OrderId equals b.OrderId
                            join c in db.Products
                            on a.ProductId equals c.ProductId
                            join d in db.Users on b.UserId equals d.UserId
                            join e in db.Cards on d.UserId equals e.UserId
                            where b.StatusId == 5 && e.UserId == session.UserId
                            select new
                            {
                                ProductId = a.ProductId,
                                Price = a.Price,
                                Quantity = a.Quantity,
                                Discount = c.Discount,
                                NumberCard = e.NumberCard,
                                Username = d.Username
                            }).ToList();
                    if (models.Count()==0)
                    {
                        ViewBag.Card = 0;
                    }
                    else
                    {
                        double? total = 0;
                        foreach (var item in models)
                        {
                            total += ((item.Price.GetValueOrDefault(0) - (item.Price.GetValueOrDefault(0) * item.Discount.GetValueOrDefault(0) * 0.01)) * item.Quantity);
                        }
                      
                        model.NumberCard = Convert.ToInt32(total / 1000)- model.UserNumber;
                        db.SaveChanges();
                        ViewBag.Card = model.NumberCard;
                    }
               
            }
           else
            {
                return PartialView();
            }
            return PartialView();

        }
        public ActionResult GoogleLogin()
        {
            var ClientID = "296238547577-pjsti6ru83ceuemopbun2q9pig434k0b.apps.googleusercontent.com";
            var url = "http://localhost:58474/RegisterAndLogin/GoogleCallback";
            var response = GoogleAuth.GetAuthUrl(ClientID, url);

            // Chuyển hướng thẳng sang Google login
            return Redirect(response);
        }

        public async Task<ActionResult> GoogleCallback(string code)
        {
            var ClientID = "296238547577-pjsti6ru83ceuemopbun2q9pig434k0b.apps.googleusercontent.com";
            var url = "http://localhost:58474/RegisterAndLogin/GoogleCallback";
            var ClientSecret = "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";

            // Lấy access token
            var token = await GoogleAuth.GetAuthAccessToken(code, ClientID, ClientSecret, url);

            // Lấy thông tin profile từ Google
            var userProfile = await GoogleAuth.GetProfileResponseAsync(token.AccessToken.ToString());

            // Parse JSON trả về thành object
            dynamic profile = Newtonsoft.Json.JsonConvert.DeserializeObject(userProfile);
            var mail = (string)profile.email;
            var ten = (string)profile.name;
            using (var db = new DBNoiThat())
            {
                // Kiểm tra user theo email
                var user = db.Users.SingleOrDefault(u => u.Email == mail);

                if (user == null)
                {
                    // Nếu chưa có thì tạo mới
                    user = new User
                    {
                        Username = mail,   // lấy email làm username
                        Email = mail,
                        Name = ten,
                        GroupId = "USER",
                        Status = true,
                        
                    };

                    db.Users.Add(user);
                    db.SaveChanges();
                }

                // Tạo session đăng nhập
                var userSession = new UserLogin
                {
                    Username = user.Username,
                    UserId = user.UserId
                };
                Session[Commoncontent.user_sesion] = userSession;

                // Đặt auth cookie
                FormsAuthentication.SetAuthCookie(user.Username, false);
            }

            return RedirectToAction("Index", "Home");
        }

        public ActionResult FacebookLogin()
        {
            var fb = new FacebookClient();
            var loginUrl = fb.GetLoginUrl(new
            {
                client_id = "835686725458313",   // hoặc appId cũng được
                redirect_uri = "http://localhost:58474/RegisterAndLogin/FacebookCallback",
                response_type = "code",
                scope = "public_profile,email"
            });

            return Redirect(loginUrl.ToString());
        }

        public async Task<ActionResult> FacebookCallback(string code)
        {
            var fb = new FacebookClient();
            dynamic result = fb.Get("/oauth/access_token", new
            {
                client_id = "835686725458313",
                client_secret = "db48f4fd7ffd226394027f05c15d395d", // LƯU Ý: Không nên để Secret trong code thật!
                redirect_uri = "http://localhost:58474/RegisterAndLogin/FacebookCallback",
                code = code 
            });
            fb.AccessToken = result.access_token;
            dynamic me = fb.Get("/me?fields=name,email");

            string name = me.name;
            string email = me.email;
            using (var db = new DBNoiThat())
            {
                var user = db.Users.SingleOrDefault(u => u.Email == email);

                if (user == null)
                {
                    user = new User
                    {
                        Username = email,
                        Email = email,
                        Name = name,
                        GroupId = "USER",
                        Status = true,
                        
                    };

                    db.Users.Add(user);
                    db.SaveChanges();
                }

                // Tạo session đăng nhập
                var userSession = new UserLogin
                {
                    Username = user.Username,
                    UserId = user.UserId
                };
                Session[Commoncontent.user_sesion] = userSession;

                // Đặt auth cookie
                FormsAuthentication.SetAuthCookie(user.Username, false);
            }
            

            return RedirectToAction("Index", "Home");
        }

        //đổi mật khẩu
        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(string email)
        {
            var user = new UserDao();
            if (!user.CheckEmail(email))
            {               
                ViewBag.Message = "Email không tồn tại!";
                return View();
            }
            
            if(!user.CheckUserGoogleFaceBook(email))
            {
                ViewBag.Message = "Email được đăng nhập bằng hình thức Google hoặc FaceBook";
                return View();
            }    
            // Tạo link reset (chứa email trực tiếp)
            var resetLink = Url.Action("ResetPassword", "RegisterAndLogin",
                new { email = email }, protocol: Request.Url.Scheme);

            // Gửi mail cho user
            SendEmail(email, "Đặt lại mật khẩu",
                $"Click vào link để đổi mật khẩu: <a href='{resetLink}'>Đặt lại mật khẩu</a>");

            ViewBag.Message = "Email đặt lại mật khẩu đã được gửi!";
            return View();
        }

        // Hàm gửi email
        private void SendEmail(string toEmail, string subject, string body)
        {
            var mail = new System.Net.Mail.MailMessage();
            mail.To.Add(toEmail);
            mail.From = new System.Net.Mail.MailAddress("daoanhvu3001@gmail.com");
            mail.Subject = subject;
            mail.Body = body;
            mail.IsBodyHtml = true;

            var smtp = new System.Net.Mail.SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new System.Net.NetworkCredential("daoanhvu3001@gmail.com", "hiru wrwo jcrl emit"), // app password Gmail
                EnableSsl = true
            };
            smtp.Send(mail);
        }
        //resetpassword
        [HttpGet]
        public ActionResult ResetPassword(string email)
        {
            // Check email tồn tại
            var userDao = new UserDao();
            if (!userDao.CheckEmail(email))
            {
                return Content("Email không hợp lệ!");
            }

            return View(model: email);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(string email, string newPassword)
        {
            var db = new DBNoiThat();
            var user = db.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                return Content("Email không hợp lệ!");
            }
            if(newPassword.Length < 4)
            {
                ViewBag.Message = "Mật khẩu phải tối thiểu 4 ký tự";
                return View();
            }    
            // Cập nhật mật khẩu mới (hash lại nếu bạn có Encryptor.MD5Hash)
            user.Password = Encryptor.MD5Hash(newPassword);

            db.SaveChanges();

            ViewBag.Message = "Mật khẩu đã được đổi thành công!";
            return RedirectToAction("Login");
        }

    }
}
//Google Soluse (Clould)
//console.cloud.google.com/auth/clients/296238547577-pjsti6ru83ceuemopbun2q9pig434k0b.apps.googleusercontent.com?project=sincere-song-473413-f4 
//developers.facebook.com/?no_redirect=1
////developers.facebook.com/apps/835686725458313/settings/basic/