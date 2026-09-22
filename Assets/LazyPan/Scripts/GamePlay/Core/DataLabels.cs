namespace LazyPan {
    /// <summary>
    /// 全项目通用数据标签 行为之间传话只认这里的词 不直接引用别的行为的常量
    /// 搬项目只带走这一页纸 谁都不欠谁
    /// </summary>
    public static class DataLabels {
        public const string Health = "Health";
        public const string MaxHealth = "MaxHealth";
        public const string Dead = "Dead";

        //开火触发器与生成物的交接词
        public const string CurrentWeapon = "CurrentWeapon";
        public const string TargetID = "TargetID";
        public const string TargetType = "TargetType";

        //三选一触发旗
        public const string WantPick = "WantPick";

        //环绕球与主人
        public const string HolderID = "HolderID";
    }
}
