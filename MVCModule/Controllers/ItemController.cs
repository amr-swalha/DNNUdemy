/*
' Copyright (c) 2017 Amr Swalha
'  All rights reserved.
' 
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
' TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
' THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
' CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
' DEALINGS IN THE SOFTWARE.
' 
*/

using System;
using System.Linq;
using System.Web.Mvc;
using Udemy.DNNCourse.MVCModule.Components;
using Udemy.DNNCourse.MVCModule.Models;
using DotNetNuke.Web.Mvc.Framework.Controllers;
using DotNetNuke.Web.Mvc.Framework.ActionFilters;
using DotNetNuke.Entities.Users;
using DotNetNuke.Framework.JavaScriptLibraries;
using DotNetNuke.Services.Social.Notifications;
using System.Collections.Generic;

namespace Udemy.DNNCourse.MVCModule.Controllers
{
    [DnnHandleError]
    public class ItemController : DnnController
    {

        public ActionResult Delete(int itemId)
        {
            ItemManager.Instance.DeleteItem(itemId, ModuleContext.ModuleId);
            return RedirectToDefaultRoute();
        }

        public ActionResult Edit(int itemId = -1)
        {
            DotNetNuke.Framework.JavaScriptLibraries.JavaScript.RequestRegistration(CommonJs.DnnPlugins);

            GetAllUsers();

            var item = (itemId == -1)
                 ? new Item { ModuleId = ModuleContext.ModuleId }
                 : ItemManager.Instance.GetItem(itemId, ModuleContext.ModuleId);

            return View(item);
        }

        [HttpPost]
        [DotNetNuke.Web.Mvc.Framework.ActionFilters.ValidateAntiForgeryToken]
        public ActionResult Edit(Item item)
        {
            if (item.ItemId == -1)
            {
                item.CreatedByUserId = User.UserID;
                item.CreatedOnDate = DateTime.UtcNow;
                item.LastModifiedByUserId = User.UserID;
                item.LastModifiedOnDate = DateTime.UtcNow;

                ItemManager.Instance.CreateItem(item);
            }
            else
            {
                var existingItem = ItemManager.Instance.GetItem(item.ItemId, item.ModuleId);
                existingItem.LastModifiedByUserId = User.UserID;
                existingItem.LastModifiedOnDate = DateTime.UtcNow;
                existingItem.ItemName = item.ItemName;
                existingItem.ItemDescription = item.ItemDescription;
                existingItem.AssignedUserId = item.AssignedUserId;

                ItemManager.Instance.UpdateItem(existingItem);
            }

            return RedirectToDefaultRoute();
        }

        [ModuleAction(ControlKey = "Edit", TitleKey = "AddItem")]
        public ActionResult Index()
        {
            
            var items = ItemManager.Instance.GetItems(ModuleContext.ModuleId)
                .Where(x => x.AssignedUserId == User.UserID);
            
            return View(items);
        }

        [NonAction]
        public void GetAllUsers()
        {
            var userlist = UserController.GetUsers(PortalSettings.PortalId);
            var users = from user in userlist.Cast<UserInfo>().ToList()
                        select new SelectListItem { Text = user.DisplayName, Value = user.UserID.ToString() };
            
            ViewBag.Users = users;
        }

        public ActionResult Add()
        {
            DotNetNuke.Framework.JavaScriptLibraries.JavaScript.RequestRegistration(CommonJs.DnnPlugins);
            GetAllUsers();
            var item = new Item { ModuleId = ModuleContext.ModuleId };
            return View(item);
        }
        [HttpPost]
        public ActionResult Add(Item item)
        {
            item.CreatedByUserId = User.UserID;
            ItemManager.Instance.CreateItem(item);
            var notification = NotificationsController.Instance.GetNotificationType("HtmlNotification");
            var notif = new Notification()
            {
                NotificationTypeID = notification.NotificationTypeId,
                Subject = "a new item has been assigned to you",
                Body = "Item: "+ item.ItemName,
                SenderUserID = User.UserID
            };
            var lstUser = new List<UserInfo>();
            lstUser.Add(UserController.GetUserById(ModuleContext.PortalId, item.AssignedUserId == -1 ? 1: item.AssignedUserId));
            NotificationsController.Instance.SendNotification(notif, ModuleContext.PortalId, null, lstUser);
            return RedirectToDefaultRoute();
        }
    }
}
