using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;

namespace Backoffice.Web.Services
{
    public class CommonService : ICommonService
    {
        public string ToShortName(int currentPeriod)
        {
            var result = string.Empty;

            switch (currentPeriod)
            {
                case 1:
                    result = "1P"; //First Period
                    break;
                case 2:
                    result = "2P"; //Second Period
                    break;
                case 3:
                    result = "3P"; //Third Period
                    break;
                case 4:
                    result = "4P"; //Fourth Period
                    break;
                case 5:
                    result = "5P"; //Fifth Period
                    break;
                case 6:
                    result = "1H"; //FirstHalf
                    break;
                case 7:
                    result = "2H"; //SecondHalf
                    break;
                case 8:
                    result = "1S"; //FirstSet
                    break;
                case 9:
                    result = "2S"; //SecondSet
                    break;
                case 10:
                    result = "3S"; //ThirdSet
                    break;
                case 11:
                    result = "4S"; //FourthSet
                    break;
                case 12:
                    result = "5S"; //FifthSet
                    break;
                case 13:
                    result = "1Q"; //FirstQuarter
                    break;
                case 14:
                    result = "2Q"; //SecondQuarter
                    break;
                case 15:
                    result = "3Q"; //ThirdQuarter
                    break;
                case 16:
                    result = "4Q"; //FourthQuarter
                    break;
                case 17:
                    result = "Golden set"; //Golden set
                    break;
                case 20:
                    result = "Started"; //Started
                    break;
                case 21:
                    result = "In progress"; //In progress
                    break;
                case 22:
                    result = "About to start"; //About to start
                    break;
                case 30:
                    result = "Break"; //Break
                    break;
                case 31:
                    result = "Halftime"; //Halftime
                    break;
                case 32:
                    result = "Awaiting extra time"; //Awaiting extra time
                    break;
                case 33:
                    result = "Extra time halftime"; //Extra time halftime
                    break;
                case 34:
                case 35:
                case 36:
                    result = "Awaiting penalties"; //Awaiting penalties
                    break;
                case 37:
                    result = "Awaiting golden set"; //Awaiting golden set
                    break;
                case 40:
                    result = "Overtime"; //Overtime
                    break;
                case 41:
                    result = "1E"; //1st extra
                    break;
                case 42:
                    result = "2E"; //2nd extra
                    break;
                case 50:
                case 51:
                case 52:
                    result = "Penalties"; //Penalties
                    break;
                case 60:
                    result = "Postponed"; //Postponed
                    break;
                case 61:
                    result = "Start delayed"; //Start delayed
                    break;
                case 70:
                    result = "Cancelled";
                    break;
                case 71:
                    result = "G1"; //Game 1
                    break;
                case 72:
                    result = "G2"; //Game 2
                    break;
                case 73:
                    result = "G3"; //Game 3
                    break;
                case 74:
                    result = "G4"; //Game 4
                    break;
                case 75:
                    result = "G5"; //Game 5
                    break;
                case 76:
                    result = "G6"; //Game 6
                    break;
                case 77:
                    result = "G7"; //Game 7
                    break;
                case 80:
                    result = "Interrupted";
                    break;
                case 81:
                    result = "Suspended";
                    break;
                case 90:
                    result = "Abandoned";
                    break;
                case 91:
                    result = "Walkover";
                    break;
                case 92:
                    result = "Retired";
                    break;
                case 93:
                    result = "Walkover, player 1 won";
                    break;
                case 94:
                    result = "Walkover, player 2 won";
                    break;
                case 95:
                    result = "Player 1 retired, player 2 won";
                    break;
                case 96:
                    result = "Player 2 retired, player 1 won";
                    break;
                case 97:
                    result = "Player 1 defaulted, player 2 won";
                    break;
                case 98:
                    result = "Player 2 defaulted, player 1 won";
                    break;
                case 99:
                    result = "Only Result";
                    break;
                case 100:
                    result = "Ended";
                    break;
                case 110:
                    result = "Awaiting extra time";
                    break;
                case 111:
                    result = "Awaiting over time";
                    break;
                case 120:
                    result = "Awaiting Penalties";
                    break;
                case 130:
                    result = "After golden set";
                    break;
                case 141:
                    result = "1st map";
                    break;
                case 142:
                    result = "2nd map";
                    break;
                case 143:
                    result = "3rd map";
                    break;
                case 144:
                    result = "4th map";
                    break;
                case 145:
                    result = "5th map";
                    break;
                case 146:
                    result = "6th map";
                    break;
                case 147:
                    result = "7th map";
                    break;
                case 151:
                    result = "1st Game";
                    break;
                case 152:
                    result = "2nd Game";
                    break;
                case 153:
                    result = "3rd Game";
                    break;
                case 154:
                    result = "4th Game";
                    break;
                case 155:
                    result = "5th Game";
                    break;
                case 161:
                    result = "1st end";
                    break;
                case 162:
                    result = "2nd end";
                    break;
                case 163:
                    result = "3rd end";
                    break;
                case 164:
                    result = "4th end";
                    break;
                case 165:
                    result = "5th end";
                    break;
                case 166:
                    result = "6th end";
                    break;
                case 167:
                    result = "7th end";
                    break;
                case 168:
                    result = "8th end";
                    break;
                case 169:
                    result = "9th end";
                    break;
                case 170:
                    result = "10th end";
                    break;
                case 171:
                    result = "Extra end";
                    break;
                case 301:
                    result = "First break";
                    break;
                case 302:
                    result = "Second break";
                    break;
                case 303:
                    result = "Third break";
                    break;
                case 304:
                    result = "Fourth break";
                    break;
                case 305:
                    result = "Fifth break";
                    break;
                case 306:
                    result = "Sixth break";
                    break;
                case 401:
                    result = "1st inning top";
                    break;
                case 402:
                    result = "1st inning bottom";
                    break;
                case 403:
                    result = "2nd inning top";
                    break;
                case 404:
                    result = "2nd inning bottom";
                    break;
                case 405:
                    result = "3rd inning top";
                    break;
                case 406:
                    result = "3rd inning bottom";
                    break;
                case 407:
                    result = "4th inning top";
                    break;
                case 408:
                    result = "4th inning bottom";
                    break;
                case 409:
                    result = "5th inning top";
                    break;
                case 410:
                    result = "5th inning bottom";
                    break;
                case 411:
                    result = "6th inning top";
                    break;
                case 412:
                    result = "6th inning bottom";
                    break;
                case 413:
                    result = "7th inning top";
                    break;
                case 414:
                    result = "7th inning bottom";
                    break;
                case 415:
                    result = "8th inning top";
                    break;
                case 416:
                    result = "8th inning bottom";
                    break;
                case 417:
                    result = "9th inning top";
                    break;
                case 418:
                    result = "9th inning bottom";
                    break;
                case 419:
                    result = "Extra inning top";
                    break;
                case 420:
                    result = "Extra inning bottom";
                    break;
                case 421:
                    result = "Break top1-bottom1";
                    break;
                case 422:
                    result = "Break top2-bottom1";
                    break;
                case 423:
                    result = "Break top2-bottom2";
                    break;
                case 424:
                    result = "Break top3-bottom2";
                    break;
                case 425:
                    result = "Break top3-bottom3";
                    break;
                case 426:
                    result = "Break top4-bottom3";
                    break;
                case 427:
                    result = "Break top4-bottom4";
                    break;
                case 428:
                    result = "Break top5-bottom4";
                    break;
                case 429:
                    result = "Break top5-bottom5";
                    break;
                case 430:
                    result = "Break top6-bottom5";
                    break;
                case 431:
                    result = "Break top6-bottom6";
                    break;
                case 432:
                    result = "Break top7-bottom6";
                    break;
                case 433:
                    result = "Break top7-bottom7";
                    break;
                case 434:
                    result = "Break top8-bottom7";
                    break;
                case 435:
                    result = "Break top8-bottom8";
                    break;
                case 436:
                    result = "Break top9-bottom8";
                    break;
                case 437:
                    result = "Break top9-bottom9";
                    break;
                case 438:
                    result = "Break topEI-bottom9";
                    break;
                case 439:
                    result = "Break topEI-bottomEI";
                    break;
                case 440:
                    result = "Sudden death";
                    break;
                case 441:
                    result = "6th set";
                    break;
                case 442:
                    result = "7th set";
                    break;
                case 443:
                    result = "Awaiting sudden death";
                    break;
                case 444:
                    result = "After sudden death";
                    break;
                case 445:
                    result = "Break";
                    break;
                case 501:
                    result = "First innings, home team";
                    break;
                case 502:
                    result = "First innings, away team";
                    break;
                case 503:
                    result = "Second innings, home team";
                    break;
                case 504:
                    result = "Second innings, away team";
                    break;
                case 505:
                    result = "Awaiting super over";
                    break;
                case 506:
                    result = "Super over, home team";
                    break;
                case 507:
                    result = "Super over, away team";
                    break;
                case 508:
                    result = "After super over";
                    break;
                case 509:
                    result = "Innings break";
                    break;
                case 510:
                    result = "Super over break";
                    break;
                case 511:
                    result = "Lunch break";
                    break;
                case 512:
                    result = "Tea break";
                    break;
                case 513:
                    result = "Stumps";
                    break;
                case 514:
                    result = "8th set";
                    break;
                case 515:
                    result = "9th set";
                    break;
                case 516:
                    result = "10th set";
                    break;
                case 517:
                    result = "11th set";
                    break;
                case 518:
                    result = "12th set";
                    break;
                case 519:
                    result = "13th set";
                    break;
                case 520:
                    result = "Third innings, home team";
                    break;
                case 521:
                    result = "Third innings, away team";
                    break;
                case 522:
                    result = "Fourth innings, home team";
                    break;
                case 523:
                    result = "Fourth innings, away team";
                    break;
                case 524:
                    result = "Dinner Break";
                    break;
                case 525:
                    result = "Drinks";
                    break;
                case 526:
                    result = "Super over";
                    break;
                case 531:
                    result = "1st inning";
                    break;
                case 532:
                    result = "2nd inning";
                    break;
                case 533:
                    result = "3rd inning";
                    break;
                case 534:
                    result = "4th inning";
                    break;
                case 535:
                    result = "5th inning";
                    break;
                case 536:
                    result = "6th inning";
                    break;
                case 537:
                    result = "7th inning";
                    break;
                case 538:
                    result = "8th inning";
                    break;
                case 539:
                    result = "9th inning";
                    break;
                default:
                    result = string.Empty;
                    break;
            }

            return result;
        }

        public decimal GetFinalOdds(decimal originalValue)
        {
            return Math.Round(originalValue, 2);
        }

        public string GetHomeName(string name, FixtureType fixtureType)
        {
            return fixtureType == FixtureType.Match ? name.Split(" vs. ").First().Trim() : string.Empty;
        }

        public string GetAwayName(string name, FixtureType fixtureType)
        {
            return fixtureType == FixtureType.Match ? name.Split(" vs. ").Last().Trim() : string.Empty;
        }

        public static string GetActivity(string dataFrom, string dataTo, Page page, string activity, bool isFrom)
        {
            var strActivity = string.Empty;

            if (page == Page.OE_Administration_MarketGroup)
            {
                if (activity == Activity.Update.ToString())
                    strActivity = isFrom ? GetDifference(Activity.Update.ToString(), dataFrom, dataTo, typeof(List<ClientMarketTemplateGroupDTO>)) : GetDifference(Activity.Update.ToString(), dataTo, dataFrom, typeof(List<ClientMarketTemplateGroupDTO>));
                else if (activity == Activity.Create.ToString())
                    strActivity = isFrom ? "-" : GetDifference(Activity.Create.ToString(), dataTo, dataFrom, typeof(List<ClientMarketTemplateGroupDTO>));
                else if (activity == Activity.Delete.ToString())
                    strActivity = isFrom ? GetDifference(Activity.Delete.ToString(), dataFrom, dataTo, typeof(ClientMarketTemplateGroupDTO)) : "-";
                else if (activity == Activity.Copy.ToString())
                    strActivity = isFrom ? GetDifference(Activity.Copy.ToString(), dataFrom, dataTo, typeof(List<ClientMarketTemplateGroupDTO>)) : GetDifference(Activity.Copy.ToString(), dataTo, dataFrom, typeof(List<ClientMarketTemplateGroupDTO>));
                else
                    strActivity = isFrom ? string.Empty : GetDifference(activity, dataTo, dataFrom, typeof(List<ClientMarketTemplateGroupDTO>));
            }
            else if (page == Page.Admin_ManageUserGroups)
            {
                if (activity == Activity.Update.ToString())
                    strActivity = isFrom ? GetDifference(Activity.Update.ToString(), dataFrom, dataTo, typeof(UserGroupResponse)) : GetDifference(Activity.Update.ToString(), dataTo, dataFrom, typeof(UserGroupResponse));
                else if (activity == Activity.Create.ToString())
                    strActivity = isFrom ? "-" : GetDifference(Activity.Create.ToString(), dataTo, dataFrom, typeof(UserGroupRequest));
                else if (activity == Activity.Delete.ToString())
                    strActivity = isFrom ? GetDifference(Activity.Delete.ToString(), dataFrom, dataTo, typeof(UserGroupResponse)) : "-";
                else
                    strActivity = isFrom ? string.Empty : GetDifference(activity, dataTo, dataFrom, typeof(UserGroupResponse));
            }
            //TODO: add another page condition here

            return strActivity;
        }

        private static string GetDifference(string activity, string data1, string data2, Type type)
        {
            var result = string.Empty;

            //Compare two object's class, return false if they are difference
            var obj = JsonSerializer.Deserialize(data1, type);

            if (obj.GetType().Name == typeof(List<ClientMarketTemplateGroupDTO>).Name)
            {
                var another = string.IsNullOrEmpty(data2) ? new List<ClientMarketTemplateGroupDTO>() : JsonSerializer.Deserialize(data2, type);

                var another2 = (List<ClientMarketTemplateGroupDTO>)another;
                var ctr = 0;
                foreach (var item in (List<ClientMarketTemplateGroupDTO>)obj)
                {
                    if (activity == Activity.Copy.ToString())
                    {
                        result = result + GetProperties(item, new ClientMarketTemplateGroupDTO()) + "||";
                    }
                    else if (activity == Activity.Update.ToString())
                    {
                        var dataResult = GetProperties(item, another2[ctr]);
                        if (!string.IsNullOrEmpty(dataResult))
                            dataResult = GetProperties(item, new ClientMarketTemplateGroupDTO());

                        result = string.IsNullOrEmpty(dataResult) ? result : result + dataResult + "||";
                    }
                    else
                    {
                        if (ctr + 1 > another2.Count)
                        {
                            result = result + GetProperties(item, new ClientMarketTemplateGroupDTO()) + "|";
                            continue;
                        }

                        if (string.IsNullOrEmpty(data2))
                            result = result + "<b>NEW*<b/><br>" + GetProperties(item, new ClientMarketTemplateGroupDTO()) + "|";
                        else
                        {
                            var dataResult = GetProperties(item, another2[ctr]);
                            result = string.IsNullOrEmpty(dataResult) ? result : result + dataResult + "|";
                        }
                    }

                    ctr++;
                }
            }
            else if (obj.GetType().Name == typeof(ClientMarketTemplateGroupDTO).Name)
            {
                var another = new object();
                if (data2 != "-")
                {
                    another = string.IsNullOrEmpty(data2) ? new ClientMarketTemplateGroupDTO() : JsonSerializer.Deserialize(data2, type);
                }

                if (string.IsNullOrEmpty(data2))
                    result = result + GetProperties(obj, another) + "|";
                else
                    result = result + GetProperties(obj, new ClientMarketTemplateGroupDTO()) + "|";

            }
            else if (obj.GetType().Name == typeof(UserGroupResponse).Name)
            {
                var another = new object();
                if (data2 != "-")
                {
                    another = string.IsNullOrEmpty(data2) ? new UserGroupResponse() : JsonSerializer.Deserialize(data2, type);
                }

                if (!string.IsNullOrEmpty(data2))
                    result = result + GetProperties(obj, another) + "|";
                else
                    result = result + GetProperties(obj, new UserGroupResponse()) + "|";

                var claims = ((UserGroupResponse)obj).Claims;
                var anotherClaims = !string.IsNullOrEmpty(data2) ? ((UserGroupResponse)another).Claims : new List<ClaimModel>();

                if (claims.Any() || anotherClaims.Any())
                {
                    foreach (var claim in claims)
                    {
                        var check = anotherClaims.FirstOrDefault(c => c.ClaimType == claim.ClaimType);
                        if (check == null)
                        {
                            if (claim.ClaimValue == "1")
                                result = result + $"{claim.ClaimType.Replace(".", " ")}<br/><div class=\"form-switch mb-2\"><input class=\"form-check-input me-4\" type=\"checkbox\" value=\"\" id=\"{claim.ClaimType}\" disabled checked></div><br/>";
                            else if (claim.ClaimValue == "2")
                                result = result + $"{claim.ClaimType.Replace(".", " ")} ReadOnly<br/><div class=\"form-switch mb-2\"><input class=\"form-check-input me-4\" type=\"checkbox\" value=\"\" id=\"{claim.ClaimType}readonly\" disabled checked></div><br/>";

                        }
                        else if (claim.ClaimValue != check.ClaimValue)
                        {
                            if (claim.ClaimType == SportsbookClientPermission.SBC_Clients_Access.ToString().Replace("_", "."))
                            {
                                var clients = claim.ClaimValue.Split(',');
                                var checkClients = check.ClaimValue.Split(',');
                                var uniqueClient = clients.Where(c => !checkClients.Contains(c));
                                var clientStr = string.Join(", ", uniqueClient);
                                if (string.IsNullOrEmpty(clientStr))
                                    result = result + $"Client Access: N/A<br/>";
                                else
                                    result = result + $"Client Access:<br/>{clientStr}<br/>";

                            }
                            else
                            {
                                if (claim.ClaimValue == "1")
                                    result = result + $"{claim.ClaimType.Replace(".", " ")}<br/><div class=\"form-switch mb-2\"><input class=\"form-check-input me-4\" type=\"checkbox\" value=\"\" id=\"{claim.ClaimType}readonly\" disabled checked></div><br/>";
                                else if (claim.ClaimValue == "2")
                                    result = result + $"{claim.ClaimType.Replace(".", " ")} ReadOnly<br/><div class=\"form-switch mb-2\"><input class=\"form-check-input me-4\" type=\"checkbox\" value=\"\" id=\"{claim.ClaimType}readonly\" disabled checked></div><br/>";
                                else
                                    result = result + $"{claim.ClaimType.Replace(".", " ")}<br/><div class=\"form-switch mb-2\"><input class=\"form-check-input me-4\" type=\"checkbox\" value=\"\" id=\"{claim.ClaimType}readonly\" disabled></div><br/>";
                            }

                        }
                    }
                }

            }
            else if (obj.GetType().Name == typeof(UserGroupRequest).Name)
            {
                var claims = ((UserGroupRequest)obj).Claims;
                
                if (claims.Any())
                {
                    foreach (var claim in claims)
                    {
                        if (claim.ClaimValue == "1")
                            result = result + $"{claim.ClaimType.Replace(".", " ")}<br/><div class=\"form-switch mb-2\"><input class=\"form-check-input me-4\" type=\"checkbox\" value=\"\" id=\"{claim.ClaimType}\" disabled checked></div><br/>";
                        else if (claim.ClaimValue == "2")
                            result = result + $"{claim.ClaimType.Replace(".", " ")} ReadOnly<br/><div class=\"form-switch mb-2\"><input class=\"form-check-input me-4\" type=\"checkbox\" value=\"\" id=\"{claim.ClaimType}readonly\" disabled checked></div><br/>";
                        else if (claim.ClaimType == SportsbookClientPermission.SBC_Clients_Access.ToString().Replace("_", "."))
                        {
                            var clients = claim.ClaimValue.Split(',');
                            var clientStr = string.Join(", ", clients);
                            if (string.IsNullOrEmpty(clientStr))
                                result = result + $"Client Access: N/A<br/>";
                            else
                                result = result + $"Client Access:<br/>{clientStr}<br/>";
                        }
                     }
                }
            }
            else
            {
                var another = string.IsNullOrEmpty(data2) ? new object() : JsonSerializer.Deserialize(data2, type);

                if (string.IsNullOrEmpty(data2))
                    result = result + GetProperties(obj, another) + "|";
                else
                    result = result + GetProperties(obj, new object()) + "|";

            }

            return result.TrimEnd('|').Replace("|", "");
        }

        private static string GetProperties(object obj, object another)
        {
            var result = string.Empty;

            //Get all properties of obj
            //And compare each other
            foreach (var property in obj.GetType().GetProperties())
            {
                var objName = property.Name;
                var objValue = property.GetValue(obj);
                var anotherValue = property.GetValue(another);
                if (objValue == null && anotherValue == null)
                    continue;

                if (objValue.GetType().Name == typeof(List<object>).Name && anotherValue.GetType().Name == typeof(List<object>).Name)
                {
                    //foreach (object item in Enumerable.OfType<objValue.GetType()>())
                    //{
                    //}
                    //foreach (var item in (objValue.GetType())objValue)
                    //{
                    //    foreach (var anotherItem in (List<object>)anotherValue)
                    //    {
                    //        //if (item.Time == anotherItem.Time)
                    //        //{
                    //        //    if (!item.Key.Equals(anotherItem.Key))
                    //        //        result = false;
                    //        //    break;
                    //        //}
                    //    }
                    //}
                }
                else if (!objValue.Equals(anotherValue))
                {
                    result = result + $"{objName}: {objValue}|";
                }
            }

            return result.TrimEnd('|').Replace("|", "<br>");
        }
    }
}
