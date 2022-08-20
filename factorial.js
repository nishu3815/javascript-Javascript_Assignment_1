function getFactorial(num)
{
    let ans=1;
    
    
    if(num==0 || num ==1)
    return num;

    for(let i = 2;i<=num;i++)
    {
        ans*=i;
    }
    return ans;

}

let num =10;
console.log(getFactorial(num));

