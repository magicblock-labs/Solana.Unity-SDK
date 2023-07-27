using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Solana.Unity.Rpc.Utilities;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
    public class UnityRateLimiter : IRateLimiter
      {
        private int _hits;
        private int _durationMS;
        private readonly Queue<DateTime> _hitList;
    
        public UnityRateLimiter(int hits, int duration_ms)
        {
          _hits = hits;
          _durationMS = duration_ms;
          _hitList = new Queue<DateTime>();
        }
    
        public static UnityRateLimiter Create() => new UnityRateLimiter(30, 1000);
    
        public bool CanFire()
        {
          DateTime utcNow = DateTime.UtcNow;
          DateTime dateTime = NextFireAllowed(utcNow);
          return utcNow >= dateTime;
        }
    
        public async void Fire()
        {
          DateTime utcNow = DateTime.UtcNow;
          DateTime dateTime = NextFireAllowed(utcNow);
          await UniTask.Delay(dateTime.Subtract(utcNow));
          if (_durationMS <= 0)
            return;
          _hitList.Enqueue(DateTime.UtcNow);
        }
    
        private DateTime NextFireAllowed(DateTime checkTime)
        {
          DateTime dateTime1 = checkTime;
          if (_durationMS == 0 || _hitList.Count == 0)
            return dateTime1;
          DateTime dateTime2 = checkTime.AddMilliseconds(-_durationMS);
          while (_hitList.Count > 0 && _hitList.Peek().Subtract(dateTime2).TotalMilliseconds < 0.0)
            _hitList.Dequeue();
          return _hitList.Count >= _hits ? _hitList.Peek().AddMilliseconds(_durationMS) : checkTime;
        }
    
        public UnityRateLimiter PerSeconds(int seconds)
        {
          _durationMS = seconds * 1000;
          return this;
        }
    
        public UnityRateLimiter PerMs(int ms)
        {
          _durationMS = ms;
          return this;
        }
    
        public UnityRateLimiter AllowHits(int hits)
        {
          _hits = hits;
          return this;
        }
    
        public override string ToString() => _hitList.Count > 0 ? string.Format("{0}-{1}", _hitList.Count, _hitList.Peek().ToString("HH:mm:ss.fff")) : "(empty)";
      }
}