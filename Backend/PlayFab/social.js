/* PlayFab Classic CloudScript. Upload this file as a revision and deploy it Live.
 * All records use Server UserInternalData; caller identity is currentPlayerId.
 * No developer secret or credential belongs in this file or the Unity client.
 */
var SOCIAL_PREFIX = "social_v1_";
var SOCIAL_LIMIT = 100;
function socialRead(player, key) {
    var data = server.GetUserInternalData({PlayFabId:player, Keys:[SOCIAL_PREFIX+key]}).Data || {};
    return data[SOCIAL_PREFIX+key] ? JSON.parse(data[SOCIAL_PREFIX+key].Value) : null;
}
function socialWrite(player, key, value) {
    var data = {}; data[SOCIAL_PREFIX+key] = JSON.stringify(value);
    server.UpdateUserInternalData({PlayFabId:player, Data:data});
}
function socialError(code) { var error = new Error(code); error.social = code; throw error; }
function socialId(value) {
    if (typeof value !== "string" || !/^[A-Za-z0-9]{1,32}$/.test(value)) socialError("not_found");
    return value.toUpperCase();
}
function socialNonce(value) {
    if (typeof value !== "string" || !/^[a-f0-9]{32}$/.test(value)) socialError("stale");
    return value;
}
function socialProfile(id) {
    var account;
    try { account = server.GetUserAccountInfo({PlayFabId:id}).UserInfo; } catch(e) { socialError("not_found"); }
    if (!account || !account.TitleInfo) socialError("not_found");
    var presence = socialRead(id,"presence"), online = !!presence && presence.state !== "offline" && Date.now()-presence.at < 60000;
    return {id:id,username:account.Username || "",name:account.TitleInfo.DisplayName || account.Username || "Oyuncu",online:online,available:online && presence.state === "menu" && !socialActive(id),presence:online?presence.state:"offline"};
}
function socialPair(a,b) {
    a=socialId(a);b=socialId(b);if(a===b)socialError("self");
    return {owner:a<b?a:b,key:"relation_"+(a<b?b:a),a:a,b:b};
}
function socialRelation(a,b) { var pair=socialPair(a,b);return socialRead(pair.owner,pair.key); }
function socialIndexes(a,b,status) {
    // Separate keys avoid overwriting unrelated simultaneous requests in an inbox blob.
    socialWrite(a,"index_"+b,{id:b,status:status});socialWrite(b,"index_"+a,{id:a,status:status});
}
function socialSaveRelation(record) {
    var pair=socialPair(record.from,record.to);
    socialWrite(pair.owner,pair.key,record);socialIndexes(record.from,record.to,record.status);
}
function socialMirror(record) {
    if (record.status === "accepted") {
        server.AddFriend({PlayFabId:record.from,FriendPlayFabId:record.to});
        server.AddFriend({PlayFabId:record.to,FriendPlayFabId:record.from});
    } else if (record.status === "removed") {
        server.RemoveFriend({PlayFabId:record.from,FriendPlayFabId:record.to});
        server.RemoveFriend({PlayFabId:record.to,FriendPlayFabId:record.from});
    }
    if(record.status === "accepted" || record.status === "removed") {
        record.mirrored=true;var pair=socialPair(record.from,record.to);
        socialWrite(pair.owner,pair.key,record);
    }
}
function socialEntries(id) {
    var data=server.GetUserInternalData({PlayFabId:id}).Data || {}, result=[];
    Object.keys(data).forEach(function(key){if(key.indexOf(SOCIAL_PREFIX+"index_")===0)result.push(JSON.parse(data[key].Value).id);});
    return result;
}
function socialCapacity(id) {
    var count=0,data=server.GetUserInternalData({PlayFabId:id}).Data || {};
    Object.keys(data).forEach(function(key){if(key.indexOf(SOCIAL_PREFIX+"index_")===0){var item=JSON.parse(data[key].Value);if(item.status==="accepted" || item.status==="pending")count++;}});
    if(count>=SOCIAL_LIMIT)socialError("limit");
}
function socialFindInvite(host,id) {
    var record=socialRead(socialId(host),"invitation");
    if(!record || record.id!==id)socialError("stale");
    if(record.status!=="started" && record.expiresAt<=Date.now() && (record.status==="pending" || record.status==="accepted")) {
        record.status="expired";socialWrite(record.from,"invitation",record);
    }
    return record;
}
function socialActive(player) {
    var pointer=socialRead(player,"match_pointer");if(!pointer)return null;
    var record=socialRead(pointer.host,"invitation");if(!record || record.id!==pointer.id)return null;
    if(record.status!=="started" && record.expiresAt<=Date.now())return null;
    return record.status==="pending" || record.status==="accepted" || record.status==="started" ? record : null;
}
function socialInviteView(record,player) {
    if(!record)return null;
    var other=record.from===player?record.to:record.from;
    var view=JSON.parse(JSON.stringify(record));view.player=socialProfile(other);return view;
}
function socialSnapshot(actor,cursor) {
    var ids=socialEntries(actor).sort(),start=Math.max(0,Math.min(ids.length,Math.floor(Number(cursor)||0))),end=Math.min(ids.length,start+5);
    var result={friends:[],incoming:[],outgoing:[],invite:null,nextCursor:end,hasMore:end<ids.length};
    ids.slice(start,end).forEach(function(other){
        var r=socialRelation(actor,other);if(!r)return;
        if((r.status==="accepted" || r.status==="removed") && !r.mirrored)socialMirror(r);
        if(r.status==="accepted")result.friends.push(socialProfile(other));
        else if(r.status==="pending") {
            var item={id:r.id,from:r.from,to:r.to,status:r.status,player:socialProfile(other)};
            (r.to===actor?result.incoming:result.outgoing).push(item);
        }
    });
    var pointer=socialRead(actor,"match_pointer");
    if(pointer)try {result.invite=socialInviteView(socialFindInvite(pointer.host,pointer.id),actor);}catch(e){if(!e.social)throw e;}
    return result;
}
handlers.Social=function(args,context) {
    try {
        var actor=socialId(currentPlayerId), action=args.action, target, record, pair;
        if(action==="presence") {
            var allowed=["menu","solo","queue","hosting","joining","match","offline"];
            if(allowed.indexOf(args.state)<0)socialError("not_allowed");
            socialWrite(actor,"presence",{state:args.state,at:Date.now()});return {ok:true};
        }
        if(action==="snapshot")return {ok:true,snapshot:socialSnapshot(actor,args.cursor)};
        if(action==="search")return {ok:true,player:socialProfile(socialId(args.target))};
        if(action==="request") {
            target=socialId(args.target);socialProfile(target);pair=socialPair(actor,target);
            record=socialRead(pair.owner,pair.key);
            if(record && record.status==="accepted")socialError("already_friends");
            if(record && record.status==="pending") {
                socialIndexes(actor,target,"pending");
                if(record.id===args.requestId && record.from===actor)return {ok:true};
                socialError("pending");
            }
            socialCapacity(actor);socialCapacity(target);
            socialSaveRelation({id:socialNonce(args.requestId),from:actor,to:target,status:"pending",createdAt:Date.now(),mirrored:false});return {ok:true};
        }
        if(action==="respond") {
            target=socialId(args.target);record=socialRelation(actor,target);
            if(!record || record.id!==args.requestId)socialError("stale");
            var decision=args.decision, status=decision==="accept"?"accepted":decision==="reject"?"rejected":decision==="cancel"?"cancelled":null;
            if(!status || (decision==="cancel"?record.from!==actor:record.to!==actor))socialError("not_allowed");
            if(record.status===status){if(!record.mirrored)socialMirror(record);return {ok:true};}
            if(record.status!=="pending")socialError("stale");
            record.status=status;record.mirrored=false;socialSaveRelation(record);socialMirror(record);return {ok:true};
        }
        if(action==="remove") {
            target=socialId(args.target);record=socialRelation(actor,target);
            if(!record || (record.status!=="accepted" && record.status!=="removed"))socialError("not_friends");
            record.status="removed";record.mirrored=false;socialSaveRelation(record);socialMirror(record);return {ok:true};
        }
        if(action==="invite") {
            if(socialRead(actor,"cancel_"+socialNonce(args.inviteId)))socialError("stale");
            target=socialId(args.target);record=socialRead(actor,"invitation");
            if(record && record.id===args.inviteId)return {ok:true,invite:socialInviteView(record,actor)};
            var friendship=socialRelation(actor,target);if(!friendship || friendship.status!=="accepted")socialError("not_friends");
            if(socialActive(actor) || socialActive(target) || !socialProfile(target).available)socialError("busy");
            if(args.region!=="eu" || !/^friend-[a-f0-9]{32}$/.test(args.room) || typeof args.category!=="number" || args.category<0 || args.category>5 || args.category%1!==0)socialError("not_allowed");
            record={id:socialNonce(args.inviteId),from:actor,to:target,room:args.room,region:"eu",category:args.category,status:"pending",expiresAt:Date.now()+60000};
            socialWrite(actor,"invitation",record);socialWrite(actor,"match_pointer",{host:actor,id:record.id});socialWrite(target,"match_pointer",{host:actor,id:record.id});
            return {ok:true,invite:socialInviteView(record,actor)};
        }
        if(action==="answerInvite") {
            record=socialFindInvite(args.host,args.inviteId);if(record.to!==actor)socialError("not_allowed");
            var wanted=args.decision==="accept"?"accepted":args.decision==="decline"?"declined":null;
            if(!wanted)socialError("not_allowed");
            if(record.status===wanted)return {ok:true,invite:socialInviteView(record,actor)};
            if(record.status==="expired")socialError("expired");if(record.status!=="pending")socialError("stale");
            var current=socialActive(actor);if(current && current.id!==record.id)socialError("busy");
            var relation=socialRelation(record.from,record.to);if(!relation || relation.status!=="accepted")socialError("not_friends");
            record.status=wanted;socialWrite(record.from,"invitation",record);return {ok:true,invite:socialInviteView(record,actor)};
        }
        if(action==="startMatch") {
            record=socialFindInvite(args.host,args.inviteId);if(record.from!==actor)socialError("not_allowed");
            if(record.status!=="accepted" && record.status!=="started")socialError(record.status==="expired"?"expired":"stale");
            var accepted=socialRelation(record.from,record.to);if(!accepted || accepted.status!=="accepted")socialError("not_friends");
            record.status="started";socialWrite(record.from,"invitation",record);return {ok:true,invite:socialInviteView(record,actor)};
        }
        if(action==="finishInvite") {
            if(socialId(args.host)===actor)socialWrite(actor,"cancel_"+socialNonce(args.inviteId),{at:Date.now()});
            var existing=socialRead(socialId(args.host),"invitation");
            if(!existing || existing.id!==args.inviteId) {
                if(socialId(args.host)===actor)return {ok:true};
                socialError("stale");
            }
            record=socialFindInvite(args.host,args.inviteId);if(actor!==record.from && actor!==record.to)socialError("not_allowed");
            record.status="cancelled";socialWrite(record.from,"invitation",record);return {ok:true};
        }
        socialError("not_allowed");
    } catch(error) {
        if(error.social)return {ok:false,error:error.social};
        // A partial reciprocal-list write is intentionally retried by snapshot reconciliation.
        log.error("Social operation failed",{action:args.action});return {ok:false,error:"backend_unavailable"};
    }
};
