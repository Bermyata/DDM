using System;

namespace IsoDataManager
{
    public class Settings
    {
        #region Static fields and Consts

        public static readonly String FileName = "IsoData.xlsx";

        public static readonly String[] Names =
        {
            "LYNX_PWR"
          , "LYNX_CONTR"
          , "LYNX_EUR_TRACEADDLINE02"
           ,"LYNX_CBLDATA_ADDROW02"
          , "LYNX_EUR_DESIGNPARAMETERS"
          , "LYNX_EUR_LINELIST_LINENO"
          , "LYNX_EUR_REFDOCS"
          , "TitleBlock"
          , "LYNX_EUR_TRACEADDLINEEQUIP*"
          , "LYNX_EUR_TRACEDATA*"
          , "LYNX_EUR_GUTTERTRACEDATA*"
          , "LYNX_EUR_ALLBOM*"
          , "LYNX_HTR*"
          , "GBL_CON*"
          , "LYNX_RTD*"
          , "LYNX_SPLT*"
          , "LYNX_EUR_ADDERCHART*"
        };

        #endregion
    }
}