
namespace Backoffice.Shared.Enums
{
    public enum Application
    {
        Backoffice = 1001,
        Sportsbook = 1002,
        AgentManagementSystem = 1003,
        ClientChecklist = 1004
    }

    public enum Application2
    {
        None = 0,
        AMS = 1, //Agent Management System
        SB = 2, //Sportsbook
        BO = 3, //BackOffice
        SBC = 4, //Sportsbook Client
        SBWL = 5, //Sportsbook Whitlabel
        AMSWL = 6 //Agent Management System Whitelabel
    }
}
