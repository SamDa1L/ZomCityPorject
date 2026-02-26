namespace ZomCity.Gameplay.Pool
{
    /// <summary>
    /// 对象池可管理对象接口。
    /// 所有需要池化的 GameObject 根节点组件必须实现此接口。
    /// </summary>
    public interface IPoolable
    {
        /// <summary>从池中取出时调用（替代 Awake/OnEnable 的初始化逻辑）。</summary>
        void OnSpawn();

        /// <summary>归还到池时调用（替代 OnDisable/OnDestroy 的清理逻辑）。</summary>
        void OnDespawn();
    }
}
