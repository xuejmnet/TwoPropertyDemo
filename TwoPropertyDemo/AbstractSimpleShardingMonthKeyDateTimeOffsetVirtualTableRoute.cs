using System;
using System.Linq.Expressions;
using ShardingCore.Core.VirtualRoutes;
using ShardingCore.Helpers;

/*
* @Author: xjm
* @Description:
* @Date: DATE TIME
* @Email: 326308290@qq.com
*/
namespace TwoPropertyDemo
{
    public abstract class AbstractSimpleShardingMonthKeyDateTimeOffsetVirtualTableRoute<TEntity> : AbstractShardingTimeKeyDateTimeOffsetVirtualTableRoute<TEntity> where TEntity : class
    {
        public abstract DateTimeOffset GetBeginTime();
        public override List<string> GetAllTails()
        {
            var beginTime = ShardingCoreHelper.GetCurrentMonthFirstDay(GetBeginTime().LocalDateTime);
         
            var tails=new List<string>();
            //提前创建表
            var nowTimeStamp =ShardingCoreHelper.GetCurrentMonthFirstDay(DateTimeOffset.Now.LocalDateTime);
            if (beginTime > nowTimeStamp)
                throw new ArgumentException("begin time error");
            var currentTimeStamp = beginTime;
            while (currentTimeStamp <= nowTimeStamp)
            {
                var tail = ShardingKeyToTail(currentTimeStamp);
                tails.Add(tail);
                currentTimeStamp = ShardingCoreHelper.GetNextMonthFirstDay(currentTimeStamp);
            }
            return tails;
        }
        protected override string TimeFormatToTail(DateTimeOffset time)
        {
            return $"{time.LocalDateTime:yyyyMM}";
        }

        public override Expression<Func<string, bool>> GetRouteToFilter(DateTimeOffset shardingKey, ShardingOperatorEnum shardingOperator)
        {
            var t = TimeFormatToTail(shardingKey);
            switch (shardingOperator)
            {
                case ShardingOperatorEnum.GreaterThan:
                case ShardingOperatorEnum.GreaterThanOrEqual:
                    return tail => String.Compare(tail, t, StringComparison.Ordinal) >= 0;
                case ShardingOperatorEnum.LessThan:
                {
                    var currentMonth = ShardingCoreHelper.GetCurrentMonthFirstDay(shardingKey.LocalDateTime);
                    //处于临界值 o=>o.time < [2021-01-01 00:00:00] 尾巴20210101不应该被返回
                    if (currentMonth == shardingKey)
                        return tail => String.Compare(tail, t, StringComparison.Ordinal) < 0;
                    return tail => String.Compare(tail, t, StringComparison.Ordinal) <= 0;
                }
                case ShardingOperatorEnum.LessThanOrEqual:
                    return tail => String.Compare(tail, t, StringComparison.Ordinal) <= 0;
                case ShardingOperatorEnum.Equal: return tail => tail == t;
                default:
                {
#if DEBUG
                    Console.WriteLine($"shardingOperator is not equal scan all table tail");
#endif
                    return tail => true;
                }
            }
        }
        public override string[] GetCronExpressions()
        {
            return new[]
            {
                "0 59 23 28,29,30,31 * ?",
                "0 0 0 1 * ?",
                "0 1 0 1 * ?",
            };
        }

    }
}