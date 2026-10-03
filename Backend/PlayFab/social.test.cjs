const fs=require('fs'),vm=require('vm'),assert=require('assert');
const state=new Map(),links=new Map();let time=1000000,failAdd=false;
const users={AA:{PlayFabId:'AA',Username:'ada',TitleInfo:{DisplayName:'Ada'}},BB:{PlayFabId:'BB',Username:'bora',TitleInfo:{DisplayName:'Bora'}},CC:{PlayFabId:'CC',Username:'cem',TitleInfo:{DisplayName:'Cem'}}};
const context={handlers:{},currentPlayerId:'AA',Date:{now:()=>time},log:{error:()=>{}},server:{
    GetUserInternalData:({PlayFabId,Keys})=>{const all=state.get(PlayFabId)||{};return {Data:Keys?Object.fromEntries(Keys.filter(k=>all[k]).map(k=>[k,all[k]])):all};},
    UpdateUserInternalData:({PlayFabId,Data})=>{const all=state.get(PlayFabId)||{};for(const [k,v] of Object.entries(Data))all[k]={Value:v};state.set(PlayFabId,all);},
    GetUserAccountInfo:({PlayFabId})=>{if(!users[PlayFabId])throw Error('missing');return {UserInfo:users[PlayFabId]};},
    AddFriend:({PlayFabId,FriendPlayFabId})=>{if(failAdd&&PlayFabId==='BB'){failAdd=false;throw Error('simulated second write failure');}const set=links.get(PlayFabId)||new Set();set.add(FriendPlayFabId);links.set(PlayFabId,set);},
    RemoveFriend:({PlayFabId,FriendPlayFabId})=>{links.get(PlayFabId)?.delete(FriendPlayFabId);}
}};
vm.createContext(context);vm.runInContext(fs.readFileSync(__dirname+'/social.js','utf8'),context);
function call(actor,action,args={}){context.currentPlayerId=actor;return JSON.parse(JSON.stringify(context.handlers.Social({action,...args},{})));}
const nonce=n=>n.toString(16).padStart(32,'0');
assert.equal(call('AA','request',{target:'AA',requestId:nonce(1)}).error,'self');
assert.equal(call('AA','search',{target:'FF'}).error,'not_found');
assert.equal(call('AA','request',{target:'BB',requestId:nonce(1)}).ok,true);
assert.equal(call('AA','request',{target:'BB',requestId:nonce(1)}).ok,true);
assert.equal(call('BB','request',{target:'AA',requestId:nonce(2)}).error,'pending');
assert.equal(call('BB','snapshot').snapshot.incoming.length,1);
assert.equal(call('AA','respond',{target:'BB',requestId:nonce(1),decision:'accept'}).error,'not_allowed');
assert.equal(call('CC','respond',{target:'AA',requestId:nonce(1),decision:'accept'}).error,'stale');
failAdd=true;
assert.equal(call('BB','respond',{target:'AA',requestId:nonce(1),decision:'accept'}).error,'backend_unavailable');
assert.equal(call('BB','snapshot').snapshot.friends.length,1);
assert(links.get('AA').has('BB')&&links.get('BB').has('AA'),'snapshot repairs partial reciprocal list write');
assert.equal(call('BB','respond',{target:'AA',requestId:nonce(1),decision:'accept'}).ok,true);
assert.equal(call('AA','request',{target:'BB',requestId:nonce(3)}).error,'already_friends');
call('AA','presence',{state:'menu'});call('BB','presence',{state:'menu'});
let invite={target:'BB',inviteId:nonce(4),room:'friend-'+nonce(5),category:3,region:'eu'};
assert.equal(call('AA','invite',invite).ok,true);
assert.equal(call('AA','invite',invite).ok,true);
assert.equal(call('BB','invite',{...invite,target:'AA',inviteId:nonce(6)}).error,'busy');
assert.equal(call('CC','answerInvite',{host:'AA',inviteId:nonce(4),decision:'accept'}).error,'not_allowed');
assert.equal(call('AA','startMatch',{host:'AA',inviteId:nonce(4)}).error,'stale');
assert.equal(call('BB','answerInvite',{host:'AA',inviteId:nonce(4),decision:'accept'}).ok,true);
assert.equal(call('AA','startMatch',{host:'AA',inviteId:nonce(4)}).invite.status,'started');
time+=70000;
assert.equal(call('AA','snapshot').snapshot.invite.status,'started');
assert.equal(call('BB','finishInvite',{host:'AA',inviteId:nonce(4)}).ok,true);
assert.equal(call('AA','snapshot').snapshot.friends[0].online,false,'presence expires after 60s');
call('AA','presence',{state:'menu'});call('BB','presence',{state:'menu'});
invite={...invite,inviteId:nonce(7)};assert.equal(call('AA','invite',invite).ok,true);time+=60001;
assert.equal(call('BB','answerInvite',{host:'AA',inviteId:nonce(7),decision:'accept'}).error,'expired');
assert.equal(call('AA','snapshot').snapshot.invite.status,'expired');
assert.equal(call('BB','remove',{target:'AA'}).ok,true);
assert(!links.get('AA').has('BB')&&!links.get('BB').has('AA'));
assert.equal(call('AA','snapshot').snapshot.friends.length,0,'removed friend cannot resurrect by old accepted record');
assert.equal(call('AA','request',{target:'BB',requestId:nonce(8)}).ok,true);
assert.equal(call('BB','respond',{target:'AA',requestId:nonce(1),decision:'accept'}).error,'stale');
assert.equal(call('BB','respond',{target:'AA',requestId:nonce(8),decision:'reject'}).ok,true);
assert.equal(call('AA','request',{target:'BB',requestId:nonce(9)}).ok,true);
assert.equal(call('AA','respond',{target:'BB',requestId:nonce(9),decision:'cancel'}).ok,true);
call('AA','presence',{state:'offline'});assert.equal(call('BB','snapshot').snapshot.incoming.length,0);
assert.equal(call('AA','finishInvite',{host:'AA',inviteId:nonce(13)}).ok,true);
assert.equal(call('AA','invite',{...invite,inviteId:nonce(13)}).error,'stale','late publish after cancellation cannot revive invite');
for(let i=0;i<7;i++){const id='D'+i;users[id]={PlayFabId:id,Username:'test'+i,TitleInfo:{DisplayName:'Test'+i}};assert.equal(call(id,'request',{target:'CC',requestId:nonce(20+i)}).ok,true);}
const first=call('CC','snapshot').snapshot,second=call('CC','snapshot',{cursor:first.nextCursor}).snapshot;
assert.equal(first.incoming.length,5);assert.equal(first.hasMore,true);assert.equal(second.incoming.length,2);assert.equal(second.hasMore,false);
console.log('PASS: offline inbox, reciprocal acceptance, repeated/cross requests, caller authorization, partial-write repair, removal, stale IDs, private match invitation acceptance/start/cancel/expiration and presence expiry. Mock Server API; live concurrency/network not tested.');
