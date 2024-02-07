using PopInPaymentForm.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PopInPaymentForm.Controllers
{
    public class PaymentController : Controller
    {
        public IActionResult Index()
        {
            var nombre = HttpContext.Request.Form["txt_nombre"];
            var apellido = HttpContext.Request.Form["txt_apellido"];
            var correo = HttpContext.Request.Form["txt_correo"];
            var valor = HttpContext.Request.Form["txt_monto"];
            int monto = (Convert.ToInt32(valor) * 100);

            Task<string> task1 = api(nombre, apellido, correo, monto);
            string valorjson = task1.Result;
            var jsondeserialze = JsonConvert.DeserializeObject<dynamic>(valorjson);
            ViewBag.token = jsondeserialze.answer.formToken;
            return View();
        }
        [HttpPost]
        public IActionResult respuesta()
        {
            var valor = HttpContext.Request.Form["kr-answer"];
            var jsondeserialze = JsonConvert.DeserializeObject<dynamic>(valor);
            ViewBag.Valor = jsondeserialze;
            return View();
        }

        [HttpPost]
        [ActionName("validate")]
        public string validate()
        {
            PaymentModel gm = new PaymentModel();
            var validate = HttpContext.Request;
            string key = "";
            var calculatedHash = "";
            if ("sha256_hmac" != validate.Form["kr_hash_algorithm"])
            {
                return JsonConvert.SerializeObject("false");
            }
            string krAnswer = validate.Form["kr_answer"].ToString().Replace("\\/", "/");
            if (validate.Form["kr_hash_key"] == "sha256_hmac")
            {
                key = gm.KEY_SHA256;
            }
            else if (validate.Form["kr_hash_key"] == "password")
            {
                key = gm.KEY_PASSWORD;
            }
            else
            {
                return JsonConvert.SerializeObject("false");
            }

            calculatedHash = hmac256(key, krAnswer);
            if (calculatedHash == validate.Form["kr_hash"]) return JsonConvert.SerializeObject("true");
            calculatedHash = hmac256(key, Decoder(krAnswer));
            if (calculatedHash == validate.Form["kr_hash"]) return JsonConvert.SerializeObject("true");
            return JsonConvert.SerializeObject("false");
        }

        public string Decoder(string value)
        {
            Encoding enc = new UTF8Encoding();
            byte[] bytes = enc.GetBytes(value);
            return enc.GetString(bytes);
        }

        public string hmac256(string key, string krAnswer)
        {
            ASCIIEncoding encoder = new ASCIIEncoding();
            Byte[] code = encoder.GetBytes(key);
            HMACSHA256 hmSha256 = new HMACSHA256(code);
            Byte[] hashMe = encoder.GetBytes(krAnswer);
            Byte[] hmBytes = hmSha256.ComputeHash(hashMe);
            return ToHexString(hmBytes);
        }

        public static string ToHexString(byte[] array)
        {
            StringBuilder hex = new StringBuilder(array.Length * 2);
            foreach (byte b in array)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }

        public String encriptado()
        {
            PaymentModel gm = new PaymentModel();
            string str = gm.KEY_USER + ":" + gm.KEY_PASSWORD;
            byte[] encbuff = System.Text.Encoding.UTF8.GetBytes(str);
            return Convert.ToBase64String(encbuff);
        }

        public async Task<String> api(String nombre, String apellido, String correo, int monto)
        {
            PaymentModel gm = new PaymentModel();
            string path = "api-payment/V4/Charge/CreatePayment";
            var objjson = new
            {
                amount = monto,
                currency = "PEN",
                orderId = "MyOrderId-123456",
                customer = new
                {
                    email = correo,
                    billingDetails = new
                    {
                        firstName = nombre,
                        lastName = apellido,
                        phoneNumber = "933819747",
                        address = "av larco",
                        address2 = "av larco"
                    }
                }
            };
            var json = JsonConvert.SerializeObject(objjson);
            var data = new StringContent(json, Encoding.UTF8, "application/json");
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Basic " + encriptado());
            var response = await client.PostAsync(gm.URL_BASE + path, data);
            string result = response.Content.ReadAsStringAsync().Result;
            client.Dispose();
            return result;
        }
    }
}

