using System;
using ShardingCore.VirtualRoutes.Abstractions;

/*
* @Author: xjm
* @Description:
* @Date: DATE TIME
* @Email: 326308290@qq.com
*/
namespace TwoPropertyDemo
{
    public abstract class AbstractShardingTimeKeyDateTimeOffsetVirtualTableRoute<TEntity> : AbstractShardingAutoCreateOperatorVirtualTableRoute<TEntity, DateTimeOffset> where TEntity : class
    {
        /// <summary>
        /// how convert sharding key to tail
        /// </summary>
        /// <param name="shardingKey"></param>
        /// <returns></returns>
        public override string ShardingKeyToTail(object shardingKey)
        {
            var time = (DateTimeOffset)shardingKey;
            return TimeFormatToTail(time);
        }
        /// <summary>
        /// how format date time to tail
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        protected abstract string TimeFormatToTail(DateTimeOffset time);

    }
}