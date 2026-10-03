using System;
using System.Collections.Generic;

[Serializable] public class SocialPlayer
{
    public string id, username, name, presence;
    public bool online, available;
}
[Serializable] public class SocialRequest
{
    public string id, from, to, status;
    public SocialPlayer player;
}
[Serializable] public class SocialInvite
{
    public string id, from, to, room, region, status;
    public int category;
    public double expiresAt;
    public SocialPlayer player;
}
[Serializable] public class SocialSnapshot
{
    public List<SocialPlayer> friends = new List<SocialPlayer>();
    public List<SocialRequest> incoming = new List<SocialRequest>(), outgoing = new List<SocialRequest>();
    public SocialInvite invite;
    public int nextCursor;
    public bool hasMore;
}
[Serializable] public class SocialReply
{
    public bool ok;
    public string error;
    public SocialPlayer player;
    public SocialSnapshot snapshot;
    public SocialInvite invite;
}
