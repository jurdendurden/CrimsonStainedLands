var i=class{constructor(){this.NewInput=new Uint8Array;this.Response=new Uint8Array}},o=class{constructor(){this.Options=[];this.SupportedClientTypes=["256COLOR","VT100","ANSI","TRUECOLOR"];this.NegotiatedClientTypes=[];this.currentTypeIndex=-1;this.ClientNegotiateTelnetType=new Uint8Array([255,250,24,0])}IsNegotiationRequired(e){return e.includes(255)}Negotiate(e){let t=new i,n=0,r=0;for(;n<e.length;)if(e[n]===255){if(t.NewInput=this.concatUint8Arrays(t.NewInput,e.slice(r,n)),n++,n>=e.length)break;let s=e[n];switch(n++,s){case 253:case 254:case 251:case 252:if(n>=e.length)break;let a=e[n];n++,t.Response=this.concatUint8Arrays(t.Response,this.handleCommand(s,a));break;case 250:let d=this.handleSubNegotiation(e.slice(n));t.Response=this.concatUint8Arrays(t.Response,d),n+=this.findSubNegotiationEnd(e.slice(n))+1;break}r=n}else n++;return t.NewInput=this.concatUint8Arrays(t.NewInput,e.slice(r)),t}handleCommand(e,t){switch(t){case 24:if(e===253||e===251)return this.SendNextClientType();break}return new Uint8Array}handleSubNegotiation(e){return e[0]===24&&e[1]===1?this.SendNextClientType():new Uint8Array}findSubNegotiationEnd(e){for(let t=0;t<e.length-1;t++)if(e[t]===255&&e[t+1]===240)return t+1;return e.length}SendNextClientType(){this.currentTypeIndex=(this.currentTypeIndex+1)%this.SupportedClientTypes.length;let e=this.SupportedClientTypes[this.currentTypeIndex],t=new TextEncoder().encode(e);return this.concatUint8Arrays(this.ClientNegotiateTelnetType,t,new Uint8Array([255,240]))}concatUint8Arrays(...e){let t=e.reduce((s,a)=>s+a.length,0),n=new Uint8Array(t),r=0;for(let s of e)n.set(s,r),r+=s.length;return n}};var l=class{constructor(e,t,n){this.negotiator=new o;this.socket=null;this.outputHandler=e,this.inputHandler=t,this.onConnectHandler=n}handleMessage(e){if(e.data instanceof ArrayBuffer){let t=new Uint8Array(e.data);this.processBinaryData(t)}else typeof e.data=="string"?this.processTextData(e.data):console.warn("Received unknown data type:",typeof e.data)}processBinaryData(e){if(this.negotiator.IsNegotiationRequired(e)){let t=this.negotiator.Negotiate(e);t.Response.length>0&&this.sendResponse(t.Response),t.NewInput.length>0&&this.outputHandler(new TextDecoder().decode(t.NewInput))}else this.outputHandler(new TextDecoder().decode(e))}processTextData(e){this.outputHandler(e)}sendResponse(e){this.socket&&this.socket.readyState===WebSocket.OPEN&&this.socket.send(e)}disconnect(){this.socket!=null&&(this.socket.close(),this.socket=null)}connect(){this.disconnect();let e=window.location.protocol==="https:"?"wss:":"ws:",t=window.location.hostname,n=4003;this.socket=new WebSocket(`${e}//${t}:${n}`),this.socket.binaryType="arraybuffer",this.socket.onmessage=this.handleMessage.bind(this),this.socket.onopen=r=>{this.outputHandler(`Connected to MUD server
`),this.onConnectHandler()},this.socket.onclose=r=>{let s=r.wasClean?`Connection closed cleanly, code=${r.code} reason=${r.reason}
`:`
Connection died
`;this.outputHandler(s)},this.socket.onerror=r=>{this.outputHandler(`
Error: ${r.message}
`)}}sendMessage(e){if(this.socket&&this.socket.readyState===WebSocket.OPEN){let t=new TextEncoder().encode(e+`
`);this.socket.send(t),this.inputHandler(e)}else this.outputHandler(`Not connected. Type /connect to connect to the MUD server.
`)}isConnected(){return this.socket!==null&&this.socket.readyState===WebSocket.OPEN}};export{l as WebSocketManager};
//# sourceMappingURL=websocket.js.map
