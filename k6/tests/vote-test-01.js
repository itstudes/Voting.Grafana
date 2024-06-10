import http from 'k6/http';
import { check, sleep } from 'k6';

const baseURL = 'http://voting.grafana:8080';

export const options = {
    stages: [
        { duration: '30s', target: 100 }, // Ramp up to 10 users
        { duration: '10m', target: 100 },  // Stay at 10 users for 1 minute
        { duration: '30s', target: 0 },  // Ramp down to 0 users
    ],
};

function getRandomInt(min, max) {
    return Math.floor(Math.random() * (max - min + 1)) + min;
}

function getRandomPercentages(num) {
    let percentages = [];
    let total = 100;

    for (let i = 0; i < num - 1; i++) {
        let randomPercentage = getRandomInt(1, total - (num - i - 1));
        percentages.push(randomPercentage);
        total -= randomPercentage;
    }

    percentages.push(total);
    return percentages;
}

function getRandomParties(forecasts, num) {
    let selectedParties = [];
    let usedIndices = new Set();

    for (let i = 0; i < num; i++) {
        let index;
        do {
            index = getRandomInt(0, forecasts.length - 1);
        } while (usedIndices.has(index));
        usedIndices.add(index);
        selectedParties.push(forecasts[index]);
    }

    return selectedParties;
}

export default function () {

    // Check the status of the current voting round
    const statusRes = http.get(`${baseURL}/manage/status`);
    check(statusRes, { 'status was 200': (r) => r.status === 200 });
    const status = JSON.parse(statusRes.body);
    //console.log(`Voting round status \nresponse: ${statusRes.status} \nbody: ${statusRes.body}`);

    // If voting is not enabled, create a new voting round
    if (!status.votingEnabled) {
        const newRoundData = {
            VotingYear: 2024,
            ApplicableCategories: [0],
            ExpectedNumberOfVoters: 10000
        };

        const newRoundRes = http.post(`${baseURL}/manage/new-round`, JSON.stringify(newRoundData), {
            headers: { 'Content-Type': 'application/json' },
        });
        //console.log(`New voting round creation \nresponse: ${newRoundRes.status} \nbody: ${newRoundRes.body}`);
        check(newRoundRes, { 'status was 200': (r) => r.status === 200 });
    }

    //populate forecasts to know which parties are available for voting
    let forecasts = [];
    if (forecasts.length === 0) {
        const res = http.get(`${baseURL}/parties/forecasts`);
        //console.log(`Forecasts data \nresponse: ${res.status} \nbody: ${res.body}`);
        check(res, { 'status was 200': (r) => r.status === 200 });
        const fetchedForecasts = JSON.parse(res.body);
        forecasts.push(...fetchedForecasts);
    }

    //submit a vote:
    //generate a new voter ID
    const idRes = http.get(`${baseURL}/misc/generate-id`);
    //console.log(`Voter ID generation \nresponse: ${idRes.status} \nbody: ${idRes.body}`);
    check(idRes, { 'status was 200': (r) => r.status === 200 });
    const voterIdNumber = idRes.body;

    //generate the voting intentions
    const numIntentions = getRandomInt(1, 3);
    const percentages = getRandomPercentages(numIntentions);
    const selectedParties = getRandomParties(forecasts, numIntentions);
    const intentions = [];
    for (let i = 0; i < numIntentions; i++) {
        intentions.push({
            partyCode: selectedParties[i].partyCode,
            intentionPercentage: percentages[i]
        });
    }

    //create the vote request
    const voteRequest = {
        voterIdNumber: voterIdNumber,
        votingIntentions: [
            {
                category: 0, // National
                intentions: intentions
            }
        ]
    };

    //submit the vote
    const res = http.post(`${baseURL}/vote`, JSON.stringify(voteRequest), {
        headers: { 'Content-Type': 'application/json' },
    });
    //console.log(`Vote submission \nrequest body: ${res.request.body} \nresponse: ${res.status} \nbody: ${res.body}`);
    check(res, { 'status was 200': (r) => r.status === 200 });

    //wait for a random time between 1 and 5 seconds
    sleep(getRandomInt(1, 5));
}
