using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public interface ITargetable
{
    float Health { get; }
    AgentType AgentType { get; }
    public void Hit(int damage);
}

