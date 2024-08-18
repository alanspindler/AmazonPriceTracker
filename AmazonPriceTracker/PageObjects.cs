
using System.IO;

namespace PageObjects
{
    public class PageObjects
    {
        public static string LabelAmazonPrice = "#apex_desktop .a-price-whole";
        public static string LabelAmazonProductName = "[class='a-size-large product-title-word-break']";

        public static string LabelKabumPrice = ".finalPrice";
        public static string LabelKabumProductName = "xpath=//div[@id='container-purchase']/div[1]/div/h1";

        
        public static string LabelPlaystationPrice = "[data-qa='mfeCtaMain#offer0#finalPrice']";        
        public static string LabelPlaystationProductName = "[data-qa='mfe-game-title#name']";
    }
}
