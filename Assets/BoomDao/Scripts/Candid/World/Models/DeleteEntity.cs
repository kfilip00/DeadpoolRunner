using worldId = System.String;
using groupId = System.String;
using entityId = System.String;
using configId = System.String;
using BlockIndex = System.UInt64;
using EdjCase.ICP.Candid.Mapping;
using EdjCase.ICP.Candid.Models;

namespace Candid.World.Models
{
	public class DeleteEntity
	{
		[CandidName("eid")]
		public entityId Eid { get; set; }

		[CandidName("gid")]
		public groupId Gid { get; set; }

		[CandidName("wid")]
		public OptionalValue<worldId> Wid { get; set; }

		public DeleteEntity(entityId eid, groupId gid, OptionalValue<worldId> wid)
		{
			this.Eid = eid;
			this.Gid = gid;
			this.Wid = wid;
		}

		public DeleteEntity()
		{
		}
	}
}