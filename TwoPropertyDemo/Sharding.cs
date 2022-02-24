using System.Globalization;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShardingCore.Core.EntityMetadatas;
using ShardingCore.Core.VirtualRoutes;
using ShardingCore.Core.VirtualRoutes.TableRoutes.Abstractions;
using ShardingCore.Core.VirtualRoutes.TableRoutes.RouteTails.Abstractions;
using ShardingCore.Sharding;
using ShardingCore.Sharding.Abstractions;
using ShardingCore.VirtualRoutes.Abstractions;

namespace TwoPropertyDemo.Domain
{
    public class Deal
    {
        public string Id { get; set; }
        public string Market { get; set; }
        public DateTimeOffset Time { get; set; }
        public string MarketAndTime => $"{Market}#{Time:yyyy}";
    }
    public class Deal1
    {
        public string Id { get; set; }
        public string Market { get; set; }
        public DateTimeOffset Time { get; set; }
    }

    public class OrderMap : IEntityTypeConfiguration<Deal>
    {
        public void Configure(EntityTypeBuilder<Deal> builder)
        {
            builder.HasKey(o => o.Id);
            builder.Property(o => o.Id).IsUnicode(false).HasMaxLength(50);
            builder.Property(o => o.Market).IsRequired().HasMaxLength(50);
            builder.Ignore(o => o.MarketAndTime);
            builder.ToTable(nameof(Deal));
        }
    }

    public class Deal1Map : IEntityTypeConfiguration<Deal1>
    {
        public void Configure(EntityTypeBuilder<Deal1> builder)
        {
            builder.HasKey(o => o.Id);
            builder.Property(o => o.Id).IsUnicode(false).HasMaxLength(50);
            builder.Property(o => o.Market).IsRequired().HasMaxLength(50);
            builder.ToTable(nameof(Deal1));
        }
    }

    public class MyDbContext:AbstractShardingDbContext,IShardingTableDbContext
    {
        public MyDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new OrderMap());
            modelBuilder.ApplyConfiguration(new Deal1Map());
        }

        public IRouteTail RouteTail { get; set; }
    }

    public class Deal1Route : AbstractSimpleShardingMonthKeyDateTimeOffsetVirtualTableRoute<Deal1>
    {
        public override void Configure(EntityMetadataTableBuilder<Deal1> builder)
        {
            builder.ShardingProperty(o => o.Time);
        }

        public override bool AutoCreateTableByTime()
        {
            return true;
        }

        public override DateTimeOffset GetBeginTime()
        {
            return new DateTimeOffset(new DateTime(2021, 1, 1));
        }
    }
    public class DealRoute : AbstractShardingOperatorVirtualTableRoute<Deal,string>
    {
        private DateTimeOffset beginTime;
        private List<string> markets = new List<string>(){"A","B","C","D"};
        public DealRoute()
        {
            beginTime = new DateTimeOffset(new DateTime(2019, 1, 1));
        }
        public override string ShardingKeyToTail(object shardingKey)
        {
            var shardingkeyValue = shardingKey.ToString();
            var values = shardingkeyValue.Split('#');
            var market = values[0];
            var time = values[1];
            return $"{market}_{time}";
        }
        //启动时获取所有
        public override List<string> GetAllTails()
        {
            var tails = new List<string>();
            foreach (var market in markets)
            {
                //提前创建表
                var nowTimeStamp = DateTimeOffset.Now;
                if (beginTime > nowTimeStamp)
                    throw new ArgumentException("begin time error");
                var currentTimeStamp = beginTime;
                while (currentTimeStamp <= nowTimeStamp)
                {
                    var tail = ShardingKeyToTail($"{market}#{currentTimeStamp:yyyy}");
                    tails.Add(tail);
                    currentTimeStamp = currentTimeStamp.AddYears(1);
                }
            }
            return tails;
        }

        public override void Configure(EntityMetadataTableBuilder<Deal> builder)
        {
            builder.ShardingProperty(o => o.MarketAndTime);
            builder.ShardingExtraProperty(o => o.Market);
            builder.ShardingExtraProperty(o => o.Time);
        }
        /// <summary>
        /// 因为MarketAndTime不参与数据库所有直接返回空即可
        /// </summary>
        /// <param name="shardingKey"></param>
        /// <param name="shardingOperator"></param>
        /// <returns></returns>
        public override Expression<Func<string, bool>> GetRouteToFilter(string shardingKey, ShardingOperatorEnum shardingOperator)
        {
            return t => true;
        }

        public override Expression<Func<string, bool>> GetExtraRouteFilter(object shardingKey, ShardingOperatorEnum shardingOperator, string shardingPropertyName)
        {
            switch (shardingPropertyName)
            {
                case nameof(Deal.Market): return GetMarketRouteToFilter(shardingKey+"", shardingOperator);
                case nameof(Deal.Time): return GetTimeRouteToFilter((DateTimeOffset)shardingKey, shardingOperator);
                default: throw new NotImplementedException(shardingPropertyName);
            }
        }
        public  Expression<Func<string, bool>> GetMarketRouteToFilter(string shardingKey, ShardingOperatorEnum shardingOperator)
        {
            switch (shardingOperator)
            {
                case ShardingOperatorEnum.Equal: return tail => tail.StartsWith(shardingKey);
                default:
                {
                    return tail => true;
                }
            }
        }
        public  Expression<Func<string, bool>> GetTimeRouteToFilter(DateTimeOffset shardingKey, ShardingOperatorEnum shardingOperator)
        {
            var t = $"{shardingKey:yyyy}"; 
            switch (shardingOperator)
            {
                case ShardingOperatorEnum.GreaterThan:
                case ShardingOperatorEnum.GreaterThanOrEqual:
                    return tail => String.Compare(tail.Split("_", System.StringSplitOptions.None)[1], t, StringComparison.Ordinal) >= 0;
                case ShardingOperatorEnum.LessThan:
                {
                    var currentYear = new DateTimeOffset(new DateTime(2021, 1, 1));
                    //处于临界值 o=>o.time < [2021-01-01 00:00:00] 尾巴20210101不应该被返回
                    if (currentYear == shardingKey)
                        return tail => String.Compare(tail.Split("_", System.StringSplitOptions.None)[1], t, StringComparison.Ordinal) < 0;
                    return tail => String.Compare(tail.Split("_", System.StringSplitOptions.None)[1], t, StringComparison.Ordinal) <= 0;
                }
                case ShardingOperatorEnum.LessThanOrEqual:
                    return tail => String.Compare(tail.Split("_", System.StringSplitOptions.None)[1], t, StringComparison.Ordinal) <= 0;
                case ShardingOperatorEnum.Equal: return tail => tail.Split("_", System.StringSplitOptions.None)[1] == t;
                default:
                {
#if DEBUG
                    Console.WriteLine($"shardingOperator is not equal scan all table tail");
#endif
                    return tail => true;
                }
            }
        }
    }
}
