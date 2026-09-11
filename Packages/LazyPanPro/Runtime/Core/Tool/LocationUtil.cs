namespace LazyPan {
    public class LocationUtil : Singleton<LocationUtil> {
        
        /// <summary>
        /// 获取随机初始点
        /// </summary>
        /// <param name="locationInformationSign">初始点</param>
        /// <returns></returns>
        public LocationInformationData GetRandomPosition(string locationInformationSign) {
            LocationInformationSetting setting = Loader.LoadLocationInfSetting(locationInformationSign);
            if (setting == null || setting.locationInformationDatas == null ||
                setting.locationInformationDatas.Count == 0) {
                LogUtil.LogErrorFormat("生成实体失败 初始点配置:{0} 信息数据为空!", locationInformationSign);
                return null;
            }

            return setting.locationInformationDatas
                [UnityEngine.Random.Range(0, setting.locationInformationDatas.Count)];
        }
        
    }
}