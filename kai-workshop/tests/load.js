import http from 'k6/http';import {check,sleep} from 'k6';import {Rate,Trend} from 'k6/metrics';
const journeyBad=new Rate('journey_bad');const journeyMs=new Trend('journey_ms');
export const options={vus:5,duration:'60s',thresholds:{journey_bad:['rate<0.01']}};
export default function(){let headers={'X-Lab-Token':__ENV.LAB_TOKEN};let r=http.get(`${__ENV.BASE_URL||'http://localhost:8080'}/api/trains`,{headers,timeout:'5s'});let valid=false;try{valid=r.status===200&&Array.isArray(r.json())&&r.json().length>0}catch{}journeyBad.add(!valid);journeyMs.add(r.timings.duration);check(r,{'correct train search':()=>valid});sleep(0.2);}
// Lab smoke threshold only. The production teaching SLO is 99.9% over 30 days.
